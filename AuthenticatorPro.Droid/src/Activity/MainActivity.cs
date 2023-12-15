// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using AndroidX.Work;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Core.Backup.Encryption;
using AuthenticatorPro.Core.Converter;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Persistence.Exception;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Extension;
using AuthenticatorPro.Droid.Interface;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.Fragment;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.QrCode;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.Internal;
using Google.Android.Material.Snackbar;
using Google.Android.Material.TextView;
using Serilog;
using Configuration = Android.Content.Res.Configuration;
using Insets = AndroidX.Core.Graphics.Insets;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;
using UriParser = AuthenticatorPro.Core.UriParser;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/MainActivityTheme", MainLauncher = true,
        Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustPan,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataSchemes = new[] { "otpauth", "otpauth-migration" })]
    public class MainActivity : AsyncActivity
    {
        private const int PermissionCameraCode = 0;
        private const int BackupReminderThresholdMinutes = 120;

        // Request codes
        private const int RequestRestore = 0;
        private const int RequestBackupFile = 1;
        private const int RequestBackupHtml = 2;
        private const int RequestBackupUriList = 3;
        private const int RequestQrCodeFromCamera = 4;
        private const int RequestQrCodeFromImage = 5;
        private const int RequestCustomIcon = 6;
        private const int RequestSettingsRecreate = 7;
        private const int RequestImportAuthenticatorPlus = 8;
        private const int RequestImportAndOtp = 9;
        private const int RequestImportFreeOtp = 10;
        private const int RequestImportFreeOtpPlus = 11;
        private const int RequestImportAegis = 12;
        private const int RequestImportBitwarden = 13;
        private const int RequestImportTwoFas = 14;
        private const int RequestImportLastPass = 15;
        private const int RequestImportWinAuth = 16;
        private const int RequestImportTotpAuthenticator = 17;
        private const int RequestImportUriList = 18;

        // Data
        private readonly ILogger _log = Log.ForContext<MainActivity>();
        private readonly Database _database;
        private readonly IEnumerable<IBackupEncryption> _backupEncryptions;

        private readonly IAuthenticatorService _authenticatorService;
        private readonly IBackupService _backupService;
        private readonly ICategoryService _categoryService;
        private readonly ICustomIconService _customIconService;
        private readonly IImportService _importService;
        private readonly IRestoreService _restoreService;

        private readonly IAuthenticatorView _authenticatorView;
        private readonly ICustomIconView _customIconView;

        private readonly IIconResolver _iconResolver;
        private readonly ICustomIconDecoder _customIconDecoder;

        // Views
        private RecyclerView _authenticatorList;
        private BottomAppBar _bottomAppBar;

        private LinearLayout _emptyStateLayout;
        private MaterialTextView _emptyMessageText;
        private LinearLayout _startLayout;

        private AuthenticatorListAdapter _authenticatorListAdapter;
        private AutoGridLayoutManager _authenticatorLayout;
        private ReorderableListTouchHelperCallback _authenticatorTouchHelperCallback;

        // State
        private SecureStorageWrapper _secureStorageWrapper;

        private Timer _timer;
        private DateTime _pauseTime;
        private DateTime _lastBackupReminderTime;

        private bool _preventBackupReminder;
        private bool _unlockFragmentOpen;
        private bool _shouldLoadFromPersistenceOnNextOpen;
        private string _customIconApplySecret;

        public MainActivity() : base(Resource.Layout.activityMain)
        {
            _database = Dependencies.Resolve<Database>();

            _iconResolver = Dependencies.Resolve<IIconResolver>();
            _customIconDecoder = Dependencies.Resolve<ICustomIconDecoder>();
            _backupEncryptions = Dependencies.ResolveAll<IBackupEncryption>();

            _categoryService = Dependencies.Resolve<ICategoryService>();
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
            _backupService = Dependencies.Resolve<IBackupService>();
            _customIconService = Dependencies.Resolve<ICustomIconService>();
            _importService = Dependencies.Resolve<IImportService>();
            _restoreService = Dependencies.Resolve<IRestoreService>();

            _authenticatorView = Dependencies.Resolve<IAuthenticatorView>();
            _customIconView = Dependencies.Resolve<ICustomIconView>();
        }

        #region Activity Lifecycle

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _secureStorageWrapper = new SecureStorageWrapper(this);

            var windowFlags = !Preferences.AllowScreenshots ? WindowManagerFlags.Secure : 0;

            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                windowFlags |= WindowManagerFlags.TranslucentStatus;
            }

            Window.SetFlags(windowFlags, windowFlags);
            RunOnUiThread(InitViews);

            if (savedInstanceState != null)
            {
                _pauseTime = new DateTime(savedInstanceState.GetLong("pauseTime"));
                _lastBackupReminderTime = new DateTime(savedInstanceState.GetLong("lastBackupReminderTime"));
            }
            else
            {
                _pauseTime = DateTime.MinValue;
                _lastBackupReminderTime = DateTime.MinValue;
            }

            if (Preferences.DefaultCategory != null)
            {
                _authenticatorView.CategoryId = Preferences.DefaultCategory;
            }

            _authenticatorView.SortMode = Preferences.SortMode;

            RunOnUiThread(InitAuthenticatorList);

            var backPressCallback = new BackPressCallback(true);
            backPressCallback.BackPressed += OnBackButtonPressed;
            OnBackPressedDispatcher.AddCallback(backPressCallback);

            _timer = new Timer { Interval = 1000, AutoReset = true };
            _timer.Elapsed += delegate { RunOnUiThread(delegate { _authenticatorListAdapter.Tick(); }); };

            _shouldLoadFromPersistenceOnNextOpen = true;

            if (Preferences.FirstLaunch)
            {
                StartActivity(typeof(IntroActivity));
            }
        }

        protected override async Task OnResumeAsync()
        {
            RunOnUiThread(delegate
            {
                // Perhaps the animation in onpause was cancelled
                _authenticatorList.Visibility = ViewStates.Invisible;
            });

            switch (await _database.IsOpenAsync(Database.Origin.Activity))
            {
                // Unlocked, no need to do anything
                case true:
                    await OnDatabaseOpened();
                    return;

                // Locked and has password, wait for unlock in unlockbottomsheet
                case false when Preferences.PasswordProtected:
                {
                    if (_unlockFragmentOpen)
                    {
                        break;
                    }

                    var fragment = new UnlockBottomSheet();
                    fragment.UnlockAttempted += OnUnlockAttempted;
                    fragment.Dismissed += async delegate
                    {
                        _unlockFragmentOpen = false;

                        if (!await _database.IsOpenAsync(Database.Origin.Activity))
                        {
                            Finish();
                        }
                    };

                    fragment.Show(SupportFragmentManager, fragment.Tag);
                    _unlockFragmentOpen = true;

                    break;
                }

                // Locked but no password, unlock now
                case false:
                {
                    try
                    {
                        await _database.OpenAsync(null, Database.Origin.Activity);
                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Error opening unprotected database");
                        ShowDatabaseErrorDialog(e);
                        return;
                    }

                    await OnDatabaseOpened();
                    break;
                }
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutLong("pauseTime", _pauseTime.Ticks);
            outState.PutLong("lastBackupReminderTime", _lastBackupReminderTime.Ticks);
        }

        protected override void OnPause()
        {
            base.OnPause();

            _timer?.Stop();
            _pauseTime = DateTime.UtcNow;

            RunOnUiThread(delegate
            {
                if (_authenticatorList != null)
                {
                    AnimUtil.FadeOutView(_authenticatorList, AnimUtil.LengthLong);
                }
            });
        }

        #endregion

        #region Activity Events

        protected override async Task OnActivityResultAsync(int requestCode, [GeneratedEnum] Result resultCode,
            Intent intent)
        {
            _preventBackupReminder = true;

            if (resultCode != Result.Ok)
            {
                return;
            }

            switch (requestCode)
            {
                case RequestSettingsRecreate:
                    Recreate();
                    break;

                case RequestRestore:
                    await RestoreFromUri(intent.Data);
                    break;

                case RequestBackupFile:
                    await BackupToFile(intent.Data);
                    break;

                case RequestBackupHtml:
                    await BackupToHtmlFile(intent.Data);
                    break;

                case RequestBackupUriList:
                    await BackupToUriListFile(intent.Data);
                    break;

                case RequestCustomIcon:
                    await SetCustomIconFromUri(intent.Data, _customIconApplySecret);
                    _customIconApplySecret = null;
                    break;

                case RequestQrCodeFromCamera:
                    await ParseQrCodeScanResult(intent.GetStringExtra("text"));
                    break;

                case RequestQrCodeFromImage:
                    await ScanQrCodeFromImage(intent.Data);
                    break;

                case RequestImportAuthenticatorPlus:
                    await ImportFromUri(new AuthenticatorPlusBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportAndOtp:
                    await ImportFromUri(new AndOtpBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportFreeOtp:
                    await ImportFromUri(new FreeOtpBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportFreeOtpPlus:
                    await ImportFromUri(new FreeOtpPlusBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportAegis:
                    await ImportFromUri(new AegisBackupConverter(_iconResolver, _customIconDecoder), intent.Data);
                    break;

                case RequestImportBitwarden:
                    await ImportFromUri(new BitwardenBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportTwoFas:
                    await ImportFromUri(new TwoFasBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportLastPass:
                    await ImportFromUri(new LastPassBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportWinAuth:
                    await ImportFromUri(new WinAuthBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportTotpAuthenticator:
                    await ImportFromUri(new TotpAuthenticatorBackupConverter(_iconResolver), intent.Data);
                    break;

                case RequestImportUriList:
                    await ImportFromUri(new UriListBackupConverter(_iconResolver), intent.Data);
                    break;
            }
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            // Force a relayout when the orientation changes
            Task.Run(async delegate
            {
                await Task.Delay(500);
                RunOnUiThread(_authenticatorListAdapter.NotifyDataSetChanged);
            });
        }

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            base.OnApplySystemBarInsets(insets);
            var bottomPadding = (int) ViewUtils.DpToPx(this, ListFabPaddingBottom) + insets.Bottom;
            _authenticatorList.SetPadding(0, 0, 0, bottomPadding);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);

            var searchItem = menu.FindItem(Resource.Id.actionSearch);
            var searchView = (SearchView) searchItem.ActionView;
            searchView.QueryHint = GetString(Resource.String.search);

            searchView.QueryTextChange += (_, e) =>
            {
                _authenticatorView.Search = e.NewText;
                _authenticatorListAdapter.NotifyDataSetChanged();
                _authenticatorTouchHelperCallback.IsLocked = e.NewText != "";
            };

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnMenuOpened(int featureId, IMenu menu)
        {
            var sortItemId = _authenticatorView.SortMode switch
            {
                SortMode.AlphabeticalAscending => Resource.Id.actionSortAZ,
                SortMode.AlphabeticalDescending => Resource.Id.actionSortZA,
                SortMode.CopyCountDescending => Resource.Id.actionSortMostCopied,
                SortMode.CopyCountAscending => Resource.Id.actionSortLeastCopied,
                _ => Resource.Id.actionSortCustom
            };

            menu.FindItem(sortItemId)?.SetChecked(true);
            return base.OnMenuOpened(featureId, menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            SortMode sortMode;

            switch (item.ItemId)
            {
                case Resource.Id.actionSortAZ:
                    sortMode = SortMode.AlphabeticalAscending;
                    break;

                case Resource.Id.actionSortZA:
                    sortMode = SortMode.AlphabeticalDescending;
                    break;

                case Resource.Id.actionSortMostCopied:
                    sortMode = SortMode.CopyCountDescending;
                    break;

                case Resource.Id.actionSortLeastCopied:
                    sortMode = SortMode.CopyCountAscending;
                    break;

                case Resource.Id.actionSortCustom:
                    sortMode = SortMode.Custom;
                    break;

                default:
                    return base.OnOptionsItemSelected(item);
            }

            if (_authenticatorView.SortMode == sortMode)
            {
                return false;
            }

            _authenticatorView.SortMode = sortMode;
            Preferences.SortMode = sortMode;
            _authenticatorListAdapter.NotifyDataSetChanged();
            item.SetChecked(true);

            return true;
        }

        private void OnBottomAppBarNavigationClick(object sender, Toolbar.NavigationClickEventArgs e)
        {
            var bundle = new Bundle();
            bundle.PutString("currentCategoryId", _authenticatorView.CategoryId);

            var fragment = new MainMenuBottomSheet { Arguments = bundle };
            fragment.CategoryClicked += async (_, id) =>
            {
                await SwitchCategory(id);
                RunOnUiThread(fragment.Dismiss);
            };

            fragment.BackupClicked += delegate
            {
                if (!_authenticatorView.AnyWithoutFilter())
                {
                    ShowSnackbar(Resource.String.noAuthenticators, Snackbar.LengthShort);
                    return;
                }

                OpenBackupMenu();
            };

            fragment.CategoriesClicked += delegate
            {
                _shouldLoadFromPersistenceOnNextOpen = true;
                StartActivity(typeof(CategoriesActivity));
            };

            fragment.IconPacksClicked += delegate { StartActivity(typeof(IconPacksActivity)); };

            fragment.SettingsClicked += delegate
            {
                StartActivityForResult(typeof(SettingsActivity), RequestSettingsRecreate);
            };

            fragment.AboutClicked += delegate
            {
                var sub = new AboutBottomSheet();

                sub.AboutClicked += delegate { StartActivity(typeof(AboutActivity)); };

                sub.SupportClicked += delegate { StartWebBrowserActivity(GetString(Resource.String.buyMeACoffee)); };

                sub.RateClicked += delegate
                {
                    var intent = new Intent(Intent.ActionView, Uri.Parse("market://details?id=" + PackageName));

                    try
                    {
                        StartActivity(intent);
                    }
                    catch (ActivityNotFoundException)
                    {
                        ShowSnackbar(Resource.String.googlePlayNotInstalledError, Snackbar.LengthShort);
                    }
                };

                sub.ViewGitHubClicked += delegate { StartWebBrowserActivity(GetString(Resource.String.githubRepo)); };

                sub.Show(SupportFragmentManager, sub.Tag);
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnBackButtonPressed(object sender, EventArgs args)
        {
            var searchBarWasClosed = false;

            RunOnUiThread(delegate
            {
                var searchItem = Toolbar?.Menu.FindItem(Resource.Id.actionSearch);

                if (searchItem == null || !searchItem.IsActionViewExpanded)
                {
                    return;
                }

                searchItem.CollapseActionView();
                searchBarWasClosed = true;
            });

            if (searchBarWasClosed)
            {
                return;
            }

            var defaultCategory = Preferences.DefaultCategory;

            if (defaultCategory == null)
            {
                if (_authenticatorView.CategoryId != null)
                {
                    await SwitchCategory(null);
                    return;
                }
            }
            else
            {
                if (_authenticatorView.CategoryId != defaultCategory)
                {
                    await SwitchCategory(defaultCategory);
                    return;
                }
            }

            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            if (requestCode == PermissionCameraCode)
            {
                if (grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    StartActivityForResult(typeof(ScanActivity), RequestQrCodeFromCamera);
                }
                else
                {
                    ShowSnackbar(Resource.String.cameraPermissionError, Snackbar.LengthShort);
                }
            }

#pragma warning disable CA1416
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
#pragma warning restore CA1416
        }

        #endregion

        #region Database

        private async void OnUnlockAttempted(object sender, string password)
        {
            var fragment = (UnlockBottomSheet) sender;
            RunOnUiThread(delegate { fragment.SetLoading(true); });

            try
            {
                await _database.OpenAsync(password, Database.Origin.Activity);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error performing unlock");
                RunOnUiThread(delegate { fragment.ShowError(); });
                return;
            }
            finally
            {
                RunOnUiThread(delegate { fragment.SetLoading(false); });
            }

            _unlockFragmentOpen = false;
            RunOnUiThread(delegate { fragment.Dismiss(); });
            await OnDatabaseOpened();
        }

        private async Task OnDatabaseOpened()
        {
            BaseApplication.AutoLockEnabled = true;

            if (_shouldLoadFromPersistenceOnNextOpen)
            {
                _shouldLoadFromPersistenceOnNextOpen = false;

                await _authenticatorView.LoadFromPersistenceAsync();
                await _customIconView.LoadFromPersistenceAsync();

                RunOnUiThread(delegate
                {
                    AnimUtil.FadeOutView(ProgressIndicator, AnimUtil.LengthShort, true);
                    _authenticatorListAdapter.NotifyDataSetChanged();
                    _authenticatorListAdapter.Tick();
                    _authenticatorList.ScheduleLayoutAnimation();
                });

                await CheckCategoryState();
            }
            else
            {
                _authenticatorView.Update();
            }

            // Handle QR code scanning from intent
            if (Intent?.Data != null)
            {
                var uri = Intent.Data;
                Intent = null;
                await ParseQrCodeScanResult(uri.ToString());
                _preventBackupReminder = true;
            }

            CheckEmptyState();
            RunOnUiThread(delegate { _authenticatorListAdapter.Tick(); });

            if (!_preventBackupReminder && Preferences.ShowBackupReminders &&
                (DateTime.UtcNow - _lastBackupReminderTime).TotalMinutes > BackupReminderThresholdMinutes)
            {
                RemindBackup();
            }

            _preventBackupReminder = false;
            TriggerAutoBackupWorker();
        }

        private void ShowDatabaseErrorDialog(Exception exception)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.databaseError);
            builder.SetTitle(Resource.String.error);
            builder.SetIcon(Resource.Drawable.baseline_warning_24);

            builder.SetNeutralButton(Resource.String.viewErrorLog, delegate
            {
                var intent = new Intent(this, typeof(ErrorActivity));
                intent.PutExtra("exception", exception.ToString());
                StartActivity(intent);
            });

            builder.SetPositiveButton(Resource.String.retry, async delegate
            {
                await _database.CloseAsync(Database.Origin.Activity);
                Recreate();
            });

            builder.SetCancelable(false);
            builder.Create().Show();
        }

        #endregion

        #region Authenticator List

        private void InitViews()
        {
            if (Preferences.DefaultCategory == null)
            {
                SupportActionBar.SetTitle(Resource.String.categoryAll);
            }
            else
            {
                SupportActionBar.SetDisplayShowTitleEnabled(false);
            }

            if (Preferences.TransparentStatusBar)
            {
                var layoutParams = (AppBarLayout.LayoutParams) ToolbarWrapLayout.LayoutParameters;
                layoutParams.ScrollFlags = AppBarLayout.LayoutParams.ScrollFlagScroll;
            }

            ProgressIndicator.Visibility = ViewStates.Visible;
            
            _bottomAppBar = FindViewById<BottomAppBar>(Resource.Id.bottomAppBar);
            _bottomAppBar.NavigationClick += OnBottomAppBarNavigationClick;
            _bottomAppBar.MenuItemClick += delegate
            {
                if (_authenticatorListAdapter == null)
                {
                    return;
                }

                Toolbar.Menu.FindItem(Resource.Id.actionSearch).ExpandActionView();
                ScrollToPosition(0);
            };

            _authenticatorList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);
            _emptyMessageText = FindViewById<MaterialTextView>(Resource.Id.textEmptyMessage);

            _startLayout = FindViewById<LinearLayout>(Resource.Id.layoutStart);

            var viewGuideButton = FindViewById<MaterialButton>(Resource.Id.buttonViewGuide);
            viewGuideButton.Click += delegate { StartActivity(typeof(GuideActivity)); };

            var importButton = FindViewById<MaterialButton>(Resource.Id.buttonImport);
            importButton.Click += delegate { OpenImportMenu(); };

            AddButton.Click += OnAddButtonClick;
        }

        private void InitAuthenticatorList()
        {
            _authenticatorListAdapter =
                new AuthenticatorListAdapter(this, _authenticatorView, _customIconView, IsDark)
                {
                    HasStableIds = true
                };

            _authenticatorListAdapter.CodeCopied += OnAuthenticatorCopied;
            _authenticatorListAdapter.MenuClicked += OnAuthenticatorMenuClicked;
            _authenticatorListAdapter.IncrementCounterClicked += OnAuthenticatorIncrementCounterClicked;
            _authenticatorListAdapter.MovementStarted += OnAuthenticatorListMovementStarted;
            _authenticatorListAdapter.MovementFinished += OnAuthenticatorListMovementFinished;

            _authenticatorList.SetAdapter(_authenticatorListAdapter);

            var viewMode = ViewModeSpecification.FromName(Preferences.ViewMode);
            _authenticatorLayout = new AutoGridLayoutManager(this, viewMode.GetMinColumnWidth());
            _authenticatorList.SetLayoutManager(_authenticatorLayout);

            _authenticatorList.AddItemDecoration(new GridSpacingItemDecoration(this, _authenticatorLayout,
                viewMode.GetSpacing(), true));
            _authenticatorList.HasFixedSize = false;

            var animation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authenticatorList.LayoutAnimation = animation;

            _authenticatorTouchHelperCallback =
                new ReorderableListTouchHelperCallback(this, _authenticatorListAdapter, _authenticatorLayout);
            var touchHelper = new ItemTouchHelper(_authenticatorTouchHelperCallback);
            touchHelper.AttachToRecyclerView(_authenticatorList);
        }

        private void OnAuthenticatorListMovementStarted(object sender, EventArgs e)
        {
            _bottomAppBar.PerformHide();
        }

        private async void OnAuthenticatorListMovementFinished(object sender, bool orderChanged)
        {
            if (!orderChanged)
            {
                RunOnUiThread(_bottomAppBar.PerformShow);
                return;
            }

            _authenticatorView.CommitRanking();

            if (_authenticatorView.CategoryId == null)
            {
                await _authenticatorService.UpdateManyAsync(_authenticatorView);
            }
            else
            {
                var authenticatorCategories = _authenticatorView.GetCurrentBindings();
                await _categoryService.UpdateManyBindingsAsync(authenticatorCategories);
            }

            if (Preferences.SortMode != SortMode.Custom)
            {
                Preferences.SortMode = SortMode.Custom;
                _authenticatorView.SortMode = SortMode.Custom;
            }

            RunOnUiThread(_bottomAppBar.PerformShow);
        }

        private async Task CheckCategoryState()
        {
            if (_authenticatorView.CategoryId == null)
            {
                return;
            }

            var category = await _categoryService.GetCategoryByIdAsync(_authenticatorView.CategoryId);

            if (category == null)
            {
                // Currently visible category has been deleted
                await SwitchCategory(null);
                return;
            }

            RunOnUiThread(delegate
            {
                SupportActionBar.SetDisplayShowTitleEnabled(true);
                SupportActionBar.Title = category.Name;
            });
        }

        private void CheckEmptyState()
        {
            if (!_authenticatorView.Any())
            {
                RunOnUiThread(delegate
                {
                    if (_emptyStateLayout.Visibility == ViewStates.Invisible)
                    {
                        AnimUtil.FadeInView(_emptyStateLayout, AnimUtil.LengthLong);
                    }

                    if (_authenticatorList.Visibility == ViewStates.Visible)
                    {
                        AnimUtil.FadeOutView(_authenticatorList, AnimUtil.LengthShort);
                    }

                    if (_authenticatorView.CategoryId == null)
                    {
                        _emptyMessageText.SetText(Resource.String.noAuthenticatorsHelp);
                        _startLayout.Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        _emptyMessageText.SetText(Resource.String.noAuthenticatorsMessage);
                        _startLayout.Visibility = ViewStates.Gone;
                    }
                });

                _timer.Stop();
            }
            else
            {
                RunOnUiThread(delegate
                {
                    if (_emptyStateLayout.Visibility == ViewStates.Visible)
                    {
                        AnimUtil.FadeOutView(_emptyStateLayout, AnimUtil.LengthShort);
                    }

                    if (_authenticatorList.Visibility == ViewStates.Invisible)
                    {
                        AnimUtil.FadeInView(_authenticatorList, AnimUtil.LengthLong);
                    }

                    var firstVisiblePos = _authenticatorLayout.FindFirstCompletelyVisibleItemPosition();
                    var lastVisiblePos = _authenticatorLayout.FindLastCompletelyVisibleItemPosition();

                    var shouldShowOverscroll =
                        firstVisiblePos >= 0 && lastVisiblePos >= 0 &&
                        (firstVisiblePos > 0 || lastVisiblePos < _authenticatorView.Count - 1);

                    _authenticatorList.OverScrollMode =
                        shouldShowOverscroll ? OverScrollMode.Always : OverScrollMode.Never;
                });

                _timer.Start();
            }
        }

        private async Task SwitchCategory(string id)
        {
            if (id == _authenticatorView.CategoryId)
            {
                CheckEmptyState();
                return;
            }

            string categoryName;

            if (id == null)
            {
                _authenticatorView.CategoryId = null;
                categoryName = GetString(Resource.String.categoryAll);
            }
            else
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                _authenticatorView.CategoryId = id;
                categoryName = category.Name;
            }

            CheckEmptyState();

            RunOnUiThread(delegate
            {
                SupportActionBar.Title = categoryName;
                _authenticatorListAdapter.NotifyDataSetChanged();
                _authenticatorList.ScheduleLayoutAnimation();
                ScrollToPosition(0, false);
                _bottomAppBar.PerformShow();
            });
        }

        private void ScrollToPosition(int position, bool smooth = true)
        {
            if (position < 0 || position > _authenticatorView.Count - 1)
            {
                return;
            }

            if (smooth)
            {
                _authenticatorList.SmoothScrollToPosition(position);
            }
            else
            {
                _authenticatorList.ScrollToPosition(position);
            }

            AppBarLayout.SetExpanded(true);
        }

        private async void OnAuthenticatorCopied(object sender, string secret)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == secret);

            if (auth == null)
            {
                return;
            }

            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("code", auth.GetCode());
            clipboard.PrimaryClip = clip;

            ShowSnackbar(Resource.String.copiedToClipboard, Snackbar.LengthShort);
            await _authenticatorService.IncrementCopyCountAsync(auth);
        }

        private void OnAuthenticatorMenuClicked(object sender, string secret)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == secret);

            if (auth == null)
            {
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("type", (int) auth.Type);
            bundle.PutLong("counter", auth.Counter);
            bundle.PutInt("copyCount", auth.CopyCount);

            var fragment = new AuthenticatorMenuBottomSheet { Arguments = bundle };

            fragment.EditClicked += delegate { OpenEditDialog(auth); };
            fragment.ChangeIconClicked += delegate { OpenIconDialog(auth); };
            fragment.AssignCategoriesClicked += async delegate { await OpenCategoriesDialog(auth); };
            fragment.ShowQrCodeClicked += delegate { OpenQrCodeDialog(auth); };
            fragment.DeleteClicked += delegate { OpenDeleteDialog(auth); };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAuthenticatorIncrementCounterClicked(object sender, string secret)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == secret);

            if (auth == null)
            {
                return;
            }

            await _authenticatorService.IncrementCounterAsync(auth);

            var position = _authenticatorView.IndexOf(auth);
            _authenticatorListAdapter.NotifyItemChanged(position);
        }

        private void OpenQrCodeDialog(Authenticator auth)
        {
            string uri;

            try
            {
                uri = auth.GetUri();
            }
            catch (NotSupportedException)
            {
                ShowSnackbar(Resource.String.qrCodeNotSupported, Snackbar.LengthShort);
                return;
            }

            var bundle = new Bundle();
            bundle.PutString("uri", uri);

            var fragment = new QrCodeBottomSheet { Arguments = bundle };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenDeleteDialog(Authenticator auth)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetIcon(Resource.Drawable.baseline_warning_24);
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                try
                {
                    await _authenticatorService.DeleteWithCategoryBindingsAsync(auth);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Error deleting category bindings for authenticator");
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                try
                {
                    await _customIconService.CullUnusedAsync();
                }
                catch (Exception e)
                {
                    _log.Error(e, "Error culling unused icons after delete");
                    // ignored
                }

                await _authenticatorView.LoadFromPersistenceAsync();
                RunOnUiThread(delegate { _authenticatorListAdapter.NotifyDataSetChanged(); });
                CheckEmptyState();

                Preferences.BackupRequired = BackupRequirement.WhenPossible;
            });

            builder.SetNegativeButton(Resource.String.cancel, delegate { });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var fragment = new AddMenuBottomSheet();
            fragment.QrCodeClicked += delegate
            {
                var hasCamera = PackageManager.HasSystemFeature(PackageManager.FeatureCamera);

                if (hasCamera)
                {
                    var subFragment = new ScanQrCodeBottomSheet();
                    subFragment.FromCameraClicked += delegate { RequestPermissionThenScanQrCode(); };
                    subFragment.FromGalleryClicked += delegate
                    {
                        StartFilePickActivity("image/*", RequestQrCodeFromImage);
                    };
                    subFragment.Show(SupportFragmentManager, subFragment.Tag);
                }
                else
                {
                    StartFilePickActivity("image/*", RequestQrCodeFromImage);
                }
            };

            fragment.EnterKeyClicked += OpenAddDialog;
            fragment.RestoreClicked += delegate { StartFilePickActivity("*/*", RequestRestore); };
            fragment.ImportClicked += delegate { OpenImportMenu(); };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        #endregion

        #region QR Code Scanning

        private async Task ScanQrCodeFromImage(Uri uri)
        {
            string result;

            try
            {
                result = await QrCodeReader.ScanImageFromFileAsync(this, uri);
            }
            catch (IOException e)
            {
                _log.Error(e, "Error picking QR code image file");
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            catch (Exception e)
            {
                _log.Error(e, "Error scanning QR code from file");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if (result == null)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            await ParseQrCodeScanResult(result);
        }

        private async Task ParseQrCodeScanResult(string uri)
        {
            if (uri.StartsWith("otpauth-migration"))
            {
                await OnOtpAuthMigrationScan(uri);
            }
            else if (uri.StartsWith("otpauth") || uri.StartsWith("motp"))
            {
                await OnUriScan(uri);
            }
            else
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            Preferences.BackupRequired = BackupRequirement.Urgent;
        }

        private async Task OnUriScan(string uri)
        {
            UriParseResult result;

            try
            {
                result = UriParser.ParseStandardUri(uri, _iconResolver);
            }
            catch (ArgumentException)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            async Task Finalise()
            {
                try
                {
                    await _authenticatorService.AddAsync(result.Authenticator);
                }
                catch (EntityDuplicateException)
                {
                    ShowSnackbar(Resource.String.duplicateAuthenticator, Snackbar.LengthShort);
                    return;
                }
                catch (Exception e)
                {
                    _log.Error(e, "Error adding authenticator");
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                if (_authenticatorView.CategoryId != null)
                {
                    var category = await _categoryService.GetCategoryByIdAsync(_authenticatorView.CategoryId);
                    await _categoryService.AddBindingAsync(result.Authenticator, category);
                }

                await _authenticatorView.LoadFromPersistenceAsync();
                CheckEmptyState();

                if (result.Authenticator.Type.GetGenerationMethod() == GenerationMethod.Time)
                {
                    ShowAutoTimeWarning();
                }

                var position = _authenticatorView.IndexOf(result.Authenticator);

                RunOnUiThread(delegate
                {
                    _authenticatorListAdapter.NotifyDataSetChanged();
                    ScrollToPosition(position);
                });

                ShowSnackbar(Resource.String.scanSuccessful, Snackbar.LengthShort);
            }

            if (result.PinLength == 0)
            {
                await Finalise();
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("length", result.PinLength);

            var fragment = new PinBottomSheet { Arguments = bundle };

            fragment.PinEntered += async (_, pin) =>
            {
                result.Authenticator.Pin = pin;
                fragment.Dismiss();
                await Finalise();
            };

            fragment.CancelClicked += delegate { };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private Task OnOtpAuthMigrationScan(string uri)
        {
            var converter = new GoogleAuthenticatorBackupConverter(_iconResolver);
            var data = Encoding.UTF8.GetBytes(uri);
            return ImportFromData(converter, data);
        }

        private void RequestPermissionThenScanQrCode()
        {
            if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            }
            else
            {
                StartActivityForResult(typeof(ScanActivity), RequestQrCodeFromCamera);
            }
        }

        #endregion

        #region Restore / Import

        private void OpenImportMenu()
        {
            var fragment = new ImportBottomSheet();
            fragment.GoogleAuthenticatorClicked += delegate
            {
                StartWebBrowserActivity(GetString(Resource.String.githubRepo) +
                                        "/wiki/Importing-from-Google-Authenticator");
            };

            // Use */* mime-type for most binary files because some files might not show on older Android versions
            // Use */* for json also, because application/json doesn't work

            fragment.AuthenticatorPlusClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportAuthenticatorPlus);
            };

            fragment.AndOtpClicked += delegate { StartFilePickActivity("*/*", RequestImportAndOtp); };

            fragment.FreeOtpClicked += delegate { StartFilePickActivity("*/*", RequestImportFreeOtp); };

            fragment.FreeOtpPlusClicked += delegate { StartFilePickActivity("*/*", RequestImportFreeOtpPlus); };

            fragment.AegisClicked += delegate { StartFilePickActivity("*/*", RequestImportAegis); };

            fragment.BitwardenClicked += delegate { StartFilePickActivity("*/*", RequestImportBitwarden); };

            fragment.WinAuthClicked += delegate { StartFilePickActivity("*/*", RequestImportWinAuth); };

            fragment.TwoFasClicked += delegate { StartFilePickActivity("*/*", RequestImportTwoFas); };

            fragment.LastPassClicked += delegate { StartFilePickActivity("*/*", RequestImportLastPass); };

            fragment.AuthyClicked += delegate
            {
                StartWebBrowserActivity(GetString(Resource.String.githubRepo) + "/wiki/Importing-from-Authy");
            };

            fragment.TotpAuthenticatorClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportTotpAuthenticator);
            };

            fragment.BlizzardAuthenticatorClicked += delegate
            {
                StartWebBrowserActivity(GetString(Resource.String.githubRepo) +
                                        "/wiki/Importing-from-Blizzard-Authenticator");
            };

            fragment.SteamClicked += delegate
            {
                StartWebBrowserActivity(GetString(Resource.String.githubRepo) + "/wiki/Importing-from-Steam");
            };

            fragment.UriListClicked += delegate { StartFilePickActivity("*/*", RequestImportUriList); };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task<RestoreResult> DecryptAndRestore(byte[] data, string password)
        {
            foreach (var encryption in _backupEncryptions.Where(e => e.CanBeDecrypted(data)))
            {
                Backup backup;

                try
                {
                    backup = await encryption.DecryptAsync(data, password);
                }
                catch (Exception e)
                {
                    _log.Warning(e, "Unable to decrypt with {Encryption}", encryption);
                    continue;
                }

                return await _restoreService.RestoreAndUpdateAsync(backup);
            }

            throw new ArgumentException("Decryption failed");
        }

        private void PromptForRestorePassword(byte[] data)
        {
            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
            var sheet = new BackupPasswordBottomSheet { Arguments = bundle };

            sheet.PasswordEntered += async (_, password) =>
            {
                sheet.SetLoading(true);

                try
                {
                    var result = await DecryptAndRestore(data, password);
                    await FinaliseRestore(result);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Error decrypting file");
                    sheet.Error = GetString(Resource.String.restoreError);
                    sheet.SetLoading(false);
                    return;
                }

                sheet.Dismiss();
            };

            sheet.Show(SupportFragmentManager, sheet.Tag);
        }

        private async Task RestoreFromUri(Uri uri)
        {
            SetLoading(true);

            byte[] data;

            try
            {
                data = await FileUtil.ReadFile(this, uri);

                if (data.Length == 0)
                {
                    throw new IOException("The file is empty");
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Error picking file to restore");
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                SetLoading(false);
                return;
            }

            var supportedEncryptions = _backupEncryptions.Where(e => e.CanBeDecrypted(data));

            if (!supportedEncryptions.Any())
            {
                ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                SetLoading(false);
                return;
            }

            try
            {
                var result = await DecryptAndRestore(data, null);
                await FinaliseRestore(result);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error decrypting file");
                PromptForRestorePassword(data);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task ImportFromData(BackupConverter converter, byte[] data)
        {
            async Task ConvertAndRestore(string password)
            {
                var (conversionResult, restoreResult) = await _importService.ImportAsync(converter, data, password);

                foreach (var failure in conversionResult.Failures)
                {
                    var message = string.Format(
                        GetString(Resource.String.importConversionError), failure.Description, failure.Error);

                    new MaterialAlertDialogBuilder(this)
                        .SetTitle(Resource.String.importIncomplete)
                        .SetMessage(message)
                        .SetIcon(Resource.Drawable.baseline_warning_24)
                        .SetPositiveButton(Resource.String.ok, delegate { })
                        .Show();
                }

                await FinaliseRestore(restoreResult);
                Preferences.BackupRequired = BackupRequirement.Urgent;
            }

            void ShowPasswordSheet()
            {
                var bundle = new Bundle();
                bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
                var sheet = new BackupPasswordBottomSheet { Arguments = bundle };

                sheet.PasswordEntered += async (_, password) =>
                {
                    sheet.SetLoading(true);

                    try
                    {
                        await ConvertAndRestore(password);
                        sheet.Dismiss();
                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Error converting backup for restore");
                        sheet.Error = GetString(Resource.String.restoreError);
                        sheet.SetLoading(false);
                    }
                };
                sheet.Show(SupportFragmentManager, sheet.Tag);
            }

            switch (converter.PasswordPolicy)
            {
                case BackupConverter.BackupPasswordPolicy.Never:
                    SetLoading(true);

                    try
                    {
                        await ConvertAndRestore(null);
                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Error converting backup for restore");
                        ShowSnackbar(Resource.String.importError, Snackbar.LengthShort);
                    }
                    finally
                    {
                        SetLoading(false);
                    }

                    break;

                case BackupConverter.BackupPasswordPolicy.Always:
                    ShowPasswordSheet();
                    break;

                case BackupConverter.BackupPasswordPolicy.Maybe:
                    try
                    {
                        await ConvertAndRestore(null);
                    }
                    catch
                    {
                        ShowPasswordSheet();
                    }

                    break;
            }
        }

        private async Task ImportFromUri(BackupConverter converter, Uri uri)
        {
            byte[] data;

            try
            {
                data = await FileUtil.ReadFile(this, uri);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error reading file for import");
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            await ImportFromData(converter, data);
        }

        private async Task FinaliseRestore(RestoreResult result)
        {
            ShowSnackbar(result.ToString(this), Snackbar.LengthShort);

            if (result.IsVoid())
            {
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();
            await _customIconView.LoadFromPersistenceAsync();

            await SwitchCategory(null);
            ShowAutoTimeWarning();

            RunOnUiThread(delegate
            {
                _authenticatorListAdapter.NotifyDataSetChanged();
                _authenticatorList.ScheduleLayoutAnimation();
            });
        }

        #endregion

        #region Backup

        private void OpenBackupMenu()
        {
            var fragment = new BackupBottomSheet();

            void ShowPicker(string mimeType, int requestCode, string fileExtension)
            {
                StartFileSaveActivity(mimeType, requestCode,
                    FormattableString.Invariant($"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{fileExtension}"));
            }

            fragment.BackupFileClicked += delegate { ShowPicker("*/*", RequestBackupFile, Backup.FileExtension); };

            fragment.BackupHtmlFileClicked += delegate
            {
                ShowPicker(HtmlBackup.MimeType, RequestBackupHtml, HtmlBackup.FileExtension);
            };

            fragment.BackupUriListClicked += delegate
            {
                ShowPicker(UriListBackup.MimeType, RequestBackupUriList, UriListBackup.FileExtension);
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task BackupToFile(Uri destination)
        {
            async Task DoBackup(string password)
            {
                var backup = await _backupService.CreateBackupAsync();
                IBackupEncryption encryption = !string.IsNullOrEmpty(password)
                    ? new StrongBackupEncryption()
                    : new NoBackupEncryption();

                try
                {
                    var data = await encryption.EncryptAsync(backup, password);
                    await FileUtil.WriteFile(this, destination, data);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Error performing backup");
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                FinaliseBackup();
            }

            if (Preferences.PasswordProtected && Preferences.DatabasePasswordBackup)
            {
                var password = _secureStorageWrapper.GetDatabasePassword();
                await DoBackup(password);
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Set);
            var fragment = new BackupPasswordBottomSheet { Arguments = bundle };

            fragment.PasswordEntered += async (sender, password) =>
            {
                fragment.SetLoading(true);
                await DoBackup(password);
                ((BackupPasswordBottomSheet) sender).Dismiss();
            };

            fragment.CancelClicked += (sender, _) =>
            {
                // TODO: Delete empty file only if we just created it
                // DocumentsContract.DeleteDocument(ContentResolver, uri);
                ((BackupPasswordBottomSheet) sender).Dismiss();
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task BackupToHtmlFile(Uri destination)
        {
            try
            {
                var backup = await _backupService.CreateHtmlBackupAsync();
                await FileUtil.WriteFile(this, destination, backup.ToString());
            }
            catch (Exception e)
            {
                _log.Error(e, "Error performing backup to HTML file");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            FinaliseBackup();
        }

        private async Task BackupToUriListFile(Uri destination)
        {
            try
            {
                var backup = await _backupService.CreateUriListBackupAsync();
                await FileUtil.WriteFile(this, destination, backup.ToString());
            }
            catch (Exception e)
            {
                _log.Error(e, "Error performing backup to URI list file");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            FinaliseBackup();
        }

        private void FinaliseBackup()
        {
            Preferences.BackupRequired = BackupRequirement.NotRequired;
            ShowSnackbar(Resource.String.saveSuccess, Snackbar.LengthLong);
        }

        private void RemindBackup()
        {
            if (!_authenticatorView.AnyWithoutFilter())
            {
                return;
            }

            if (Preferences.BackupRequired != BackupRequirement.Urgent || Preferences.AutoBackupEnabled)
            {
                return;
            }

            _lastBackupReminderTime = DateTime.UtcNow;
            var snackbar = Snackbar.Make(RootLayout, Resource.String.backupReminder, Snackbar.LengthLong);
            snackbar.SetAnchorView(AddButton);
            snackbar.SetAction(Resource.String.backupNow, delegate { OpenBackupMenu(); });

            var callback = new SnackbarCallback();
            callback.Dismissed += (_, e) =>
            {
                if (e == Snackbar.Callback.DismissEventSwipe)
                {
                    Preferences.BackupRequired = BackupRequirement.NotRequired;
                }
            };

            snackbar.AddCallback(callback);
            snackbar.Show();
        }

        private void TriggerAutoBackupWorker()
        {
            if (!Preferences.AutoBackupEnabled && !Preferences.AutoRestoreEnabled)
            {
                return;
            }

            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            var manager = WorkManager.GetInstance(this);
            manager.EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace!, request);
        }

        #endregion

        #region Add Dialog

        private void OpenAddDialog(object sender, EventArgs e)
        {
            var fragment = new AddAuthenticatorBottomSheet();
            fragment.SubmitClicked += OnAddDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAddDialogSubmit(object sender,
            InputAuthenticatorBottomSheet.InputAuthenticatorEventArgs args)
        {
            var dialog = (AddAuthenticatorBottomSheet) sender;

            try
            {
                if (_authenticatorView.CategoryId == null)
                {
                    await _authenticatorService.AddAsync(args.Authenticator);
                }
                else
                {
                    await _authenticatorService.AddAsync(args.Authenticator);

                    var category = await _categoryService.GetCategoryByIdAsync(_authenticatorView.CategoryId);
                    await _categoryService.AddBindingAsync(args.Authenticator, category);
                }
            }
            catch (EntityDuplicateException)
            {
                dialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }
            catch (Exception e)
            {
                _log.Error(e, "Error adding authenticator");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();
            CheckEmptyState();

            if (args.Authenticator.Type.GetGenerationMethod() == GenerationMethod.Time)
            {
                ShowAutoTimeWarning();
            }

            var position = _authenticatorView.IndexOf(args.Authenticator);

            RunOnUiThread(delegate
            {
                _authenticatorListAdapter.NotifyDataSetChanged();
                ScrollToPosition(position);
            });

            dialog.Dismiss();
            Preferences.BackupRequired = BackupRequirement.Urgent;
        }

        #endregion

        #region Edit Dialog

        private void OpenEditDialog(Authenticator auth)
        {
            var bundle = new Bundle();
            bundle.PutInt("type", (int) auth.Type);
            bundle.PutString("issuer", auth.Issuer);
            bundle.PutString("username", auth.Username);
            bundle.PutString("secret", auth.Secret);
            bundle.PutString("pin", auth.Pin);
            bundle.PutInt("algorithm", (int) auth.Algorithm);
            bundle.PutInt("digits", auth.Digits);
            bundle.PutInt("period", auth.Period);
            bundle.PutLong("counter", auth.Counter);

            var fragment = new EditAuthenticatorBottomSheet { Arguments = bundle };
            fragment.SubmitClicked += OnEditDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnEditDialogSubmit(object sender,
            InputAuthenticatorBottomSheet.InputAuthenticatorEventArgs args)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == args.InitialSecret);

            if (auth == null)
            {
                return;
            }

            var dialog = (EditAuthenticatorBottomSheet) sender;
            var position = _authenticatorView.IndexOf(auth);

            auth.Type = args.Authenticator.Type;
            auth.Issuer = args.Authenticator.Issuer;
            auth.Username = args.Authenticator.Username;
            auth.Pin = args.Authenticator.Pin;
            auth.Algorithm = args.Authenticator.Algorithm;
            auth.Digits = args.Authenticator.Digits;
            auth.Period = args.Authenticator.Period;
            auth.Counter = args.Authenticator.Counter;

            try
            {
                if (args.InitialSecret != args.Authenticator.Secret)
                {
                    auth.Secret = args.InitialSecret;
                    await _authenticatorService.ChangeSecretAsync(auth, args.Authenticator.Secret);
                    auth.Secret = args.Authenticator.Secret;
                }

                await _authenticatorService.UpdateAsync(auth);
            }
            catch (EntityDuplicateException)
            {
                dialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }
            catch (Exception e)
            {
                _log.Error(e, "Error editing authenticator");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();

            if (args.Authenticator.Type.GetGenerationMethod() == GenerationMethod.Time)
            {
                ShowAutoTimeWarning();
            }

            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(position); });
            Preferences.BackupRequired = BackupRequirement.Urgent;

            dialog.Dismiss();
        }

        #endregion

        #region Icon Dialog

        private void OpenIconDialog(Authenticator auth)
        {
            var bundle = new Bundle();
            bundle.PutString("secret", auth.Secret);

            var fragment = new ChangeIconBottomSheet { Arguments = bundle };
            fragment.DefaultIconSelected += OnDefaultIconSelected;
            fragment.IconPackEntrySelected += OnIconPackEntrySelected;
            fragment.UseCustomIconClick += delegate
            {
                _customIconApplySecret = auth.Secret;
                StartFilePickActivity("image/*", RequestCustomIcon);
            };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnDefaultIconSelected(object sender, ChangeIconBottomSheet.DefaultIconSelectedEventArgs args)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == args.Secret);

            if (auth == null)
            {
                return;
            }

            var oldIcon = auth.Icon;

            try
            {
                await _authenticatorService.SetIconAsync(auth, args.Icon);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error setting authenticator icon");
                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            Preferences.BackupRequired = BackupRequirement.WhenPossible;
            var position = _authenticatorView.IndexOf(auth);
            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(position); });

            ((ChangeIconBottomSheet) sender).Dismiss();
        }

        private async void OnIconPackEntrySelected(object sender,
            ChangeIconBottomSheet.IconPackEntrySelectedEventArgs args)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == args.Secret);

            if (auth == null)
            {
                return;
            }

            SetLoading(true);
            var stream = new MemoryStream();

            try
            {
                await args.Icon.CompressAsync(Bitmap.CompressFormat.Png, 100, stream);
                var icon = await _customIconDecoder.DecodeAsync(stream.ToArray(), false);
                await SetCustomIcon(auth, icon);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error loading icon from icon pack");
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
            }
            finally
            {
                stream.Close();
                SetLoading(false);
            }

            ((ChangeIconBottomSheet) sender).Dismiss();
        }

        #endregion

        #region Custom Icons

        private async Task SetCustomIconFromUri(Uri source, string secret)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == secret);

            if (auth == null)
            {
                return;
            }

            SetLoading(true);

            try
            {
                var data = await FileUtil.ReadFile(this, source);
                var icon = await _customIconDecoder.DecodeAsync(data, true);
                await SetCustomIcon(auth, icon);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error decoding custom icon");
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task SetCustomIcon(Authenticator auth, CustomIcon icon)
        {
            var oldIcon = auth.Icon;

            try
            {
                await _authenticatorService.SetCustomIconAsync(auth, icon);
            }
            catch (Exception e)
            {
                _log.Error(e, "Error setting custom icon");
                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            await _customIconView.LoadFromPersistenceAsync();
            Preferences.BackupRequired = BackupRequirement.WhenPossible;

            var position = _authenticatorView.IndexOf(auth);
            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(position); });
        }

        #endregion

        #region Categories

        private async Task OpenCategoriesDialog(Authenticator auth)
        {
            var bindings = await _categoryService.GetBindingsForAuthenticatorAsync(auth);
            var categoryIds = bindings.Select(ac => ac.CategoryId).ToArray();

            var bundle = new Bundle();
            bundle.PutString("secret", auth.Secret);
            bundle.PutStringArray("assignedCategoryIds", categoryIds);

            var fragment = new AssignCategoriesBottomSheet { Arguments = bundle };
            fragment.CategoryClicked += OnCategoriesDialogCategoryClicked;
            fragment.EditCategoriesClicked += delegate
            {
                _shouldLoadFromPersistenceOnNextOpen = true;
                StartActivity(typeof(CategoriesActivity));
                fragment.Dismiss();
            };
            fragment.Closed += OnCategoriesDialogClosed;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnCategoriesDialogClosed(object sender, EventArgs e)
        {
            await _authenticatorView.LoadFromPersistenceAsync();

            if (_authenticatorView.CategoryId == null)
            {
                return;
            }

            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyDataSetChanged(); });

            CheckEmptyState();
        }

        private async void OnCategoriesDialogCategoryClicked(object sender,
            AssignCategoriesBottomSheet.CategoryClickedEventArgs args)
        {
            var auth = _authenticatorView.FirstOrDefault(a => a.Secret == args.Secret);

            if (auth == null)
            {
                return;
            }

            var category = await _categoryService.GetCategoryByIdAsync(args.CategoryId);

            try
            {
                if (args.IsChecked)
                {
                    await _categoryService.AddBindingAsync(auth, category);
                }
                else
                {
                    await _categoryService.RemoveBindingAsync(auth, category);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Error adding/removing category");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
            }
        }

        #endregion
        
        #region Misc

        private void ShowAutoTimeWarning()
        {
            var autoTimeEnabled = Settings.Global.GetInt(ContentResolver, Settings.Global.AutoTime) == 1;

            if (autoTimeEnabled || Preferences.ShownAutoTimeWarning)
            {
                return;
            }

            new MaterialAlertDialogBuilder(this)
                .SetTitle(Resource.String.autoTimeWarningTitle)
                .SetMessage(Resource.String.autoTimeWarningMessage)
                .SetIcon(Resource.Drawable.baseline_warning_24)
                .SetPositiveButton(Resource.String.ok, delegate { })
                .Show();

            Preferences.ShownAutoTimeWarning = true;
        }
        
        #endregion
    }
}