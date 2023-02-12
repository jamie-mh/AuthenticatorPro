// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AndroidX.RecyclerView.Widget;
using AndroidX.Work;
using AuthenticatorPro.Droid.Adapter;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Fragment;
using AuthenticatorPro.Droid.LayoutManager;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Droid.Worker;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Backup;
using AuthenticatorPro.Shared.Data.Backup.Converter;
using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using AuthenticatorPro.Shared.Service;
using AuthenticatorPro.Shared.View;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Internal;
using Google.Android.Material.Snackbar;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Configuration = Android.Content.Res.Configuration;
using Logger = AuthenticatorPro.Droid.Util.Logger;
using Result = Android.App.Result;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using Timer = System.Timers.Timer;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;

#if FDROID
using ZXing;
using ZXing.Common;
using Java.Nio;
using System.Collections.Generic;
#else
using Xamarin.Google.MLKit.Vision.BarCode;
using Xamarin.Google.MLKit.Vision.Barcode.Common;
using Xamarin.Google.MLKit.Vision.Common;
using Android.Gms.Extensions;
#endif

namespace AuthenticatorPro.Droid.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/MainActivityTheme", MainLauncher = true,
        Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustPan,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataSchemes = new[] { "otpauth", "otpauth-migration" })]
    internal class MainActivity : AsyncActivity, IOnApplyWindowInsetsListener
    {
        private const int PermissionCameraCode = 0;

        private const int BackupReminderThresholdMinutes = 120;
        private const int ListPaddingBottom = 80;

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
        private const int RequestImportFreeOtpPlus = 10;
        private const int RequestImportAegis = 11;
        private const int RequestImportBitwarden = 12;
        private const int RequestImportTwoFas = 13;
        private const int RequestImportWinAuth = 14;
        private const int RequestImportTotpAuthenticator = 15;
        private const int RequestImportUriList = 16;

        // Views
        private CoordinatorLayout _coordinatorLayout;
        private AppBarLayout _appBarLayout;
        private MaterialToolbar _toolbar;
        private ProgressBar _progressBar;
        private RecyclerView _authenticatorList;
        private FloatingActionButton _addButton;
        private BottomAppBar _bottomAppBar;

        private LinearLayout _emptyStateLayout;
        private TextView _emptyMessageText;
        private LinearLayout _startLayout;

        private AuthenticatorListAdapter _authenticatorListAdapter;
        private AutoGridLayoutManager _authenticatorLayout;
        private ReorderableListTouchHelperCallback _authenticatorTouchHelperCallback;

        // Data
        private readonly Database _database;

        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly ICustomIconRepository _customIconRepository;

        private readonly IAuthenticatorCategoryService _authenticatorCategoryService;
        private readonly IAuthenticatorService _authenticatorService;
        private readonly IBackupService _backupService;
        private readonly ICustomIconService _customIconService;
        private readonly IImportService _importService;
        private readonly IRestoreService _restoreService;
        private readonly IQrCodeService _qrCodeService;

        private readonly IAuthenticatorView _authenticatorView;

        // State
        private readonly IIconResolver _iconResolver;
        private readonly ICustomIconDecoder _customIconDecoder;
        private PreferenceWrapper _preferences;

        private Timer _timer;
        private DateTime _pauseTime;
        private DateTime _lastBackupReminderTime;

        private bool _preventBackupReminder;
        private bool _unlockFragmentOpen;
        private bool _shouldLoadFromPersistenceOnNextOpen;
        private int _customIconApplyPosition;

        public MainActivity() : base(Resource.Layout.activityMain)
        {
            _iconResolver = Dependencies.Resolve<IIconResolver>();
            _customIconDecoder = Dependencies.Resolve<ICustomIconDecoder>();
            _database = Dependencies.Resolve<Database>();

            _authenticatorCategoryService = Dependencies.Resolve<IAuthenticatorCategoryService>();
            _categoryRepository = Dependencies.Resolve<ICategoryRepository>();
            _authenticatorCategoryRepository = Dependencies.Resolve<IAuthenticatorCategoryRepository>();
            _customIconRepository = Dependencies.Resolve<ICustomIconRepository>();

            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
            _backupService = Dependencies.Resolve<IBackupService>();
            _customIconService = Dependencies.Resolve<ICustomIconService>();
            _importService = Dependencies.Resolve<IImportService>();
            _restoreService = Dependencies.Resolve<IRestoreService>();
            _qrCodeService = Dependencies.Resolve<IQrCodeService>();

            _authenticatorView = Dependencies.Resolve<IAuthenticatorView>();
        }

        #region Activity Lifecycle

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Platform.Init(this, savedInstanceState);
            _preferences = new PreferenceWrapper(this);

            var windowFlags = WindowManagerFlags.Secure;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                if (_preferences.TransparentStatusBar)
                {
                    Window.SetStatusBarColor(Color.Transparent);
                }

                Window.SetDecorFitsSystemWindows(false);
                Window.SetNavigationBarColor(Color.Transparent);

                if (!IsDark)
                {
                    Window.InsetsController?.SetSystemBarsAppearance(
                        (int) WindowInsetsControllerAppearance.LightStatusBars,
                        (int) WindowInsetsControllerAppearance.LightStatusBars);
                }
            }
            else if (_preferences.TransparentStatusBar)
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

            if (_preferences.DefaultCategory != null)
            {
                _authenticatorView.CategoryId = _preferences.DefaultCategory;
            }

            _authenticatorView.SortMode = _preferences.SortMode;

            RunOnUiThread(InitAuthenticatorList);

            var backPressCallback = new BackPressCallback(true);
            backPressCallback.BackPressed += OnBackButtonPressed;
            OnBackPressedDispatcher.AddCallback(backPressCallback);

            _timer = new Timer { Interval = 1000, AutoReset = true };
            _timer.Elapsed += delegate
            {
                RunOnUiThread(delegate
                {
                    _authenticatorListAdapter.Tick();
                });
            };

            _shouldLoadFromPersistenceOnNextOpen = true;

            if (_preferences.FirstLaunch)
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

            switch (await _database.IsOpen(Database.Origin.Activity))
            {
                // Unlocked, no need to do anything
                case true:
                    await OnDatabaseOpened();
                    return;

                // Locked and has password, wait for unlock in unlockbottomsheet
                case false when _preferences.PasswordProtected:
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

                        if (!await _database.IsOpen(Database.Origin.Activity))
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
                        await _database.Open(null, Database.Origin.Activity);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Database not usable? error: {e}");
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
                    await SetCustomIcon(intent.Data, _customIconApplyPosition);
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

        public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insets)
        {
            var systemBarInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());

            var layout = FindViewById<LinearLayout>(Resource.Id.toolbarWrapLayout);
            layout.SetPadding(0, systemBarInsets.Top, 0, 0);

            var bottomPadding = (int) ViewUtils.DpToPx(this, ListPaddingBottom) + systemBarInsets.Bottom;
            _authenticatorList.SetPadding(0, 0, 0, bottomPadding);

            return insets;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);

            var searchItem = menu.FindItem(Resource.Id.actionSearch);
            var searchView = (SearchView) searchItem.ActionView;
            searchView.QueryHint = GetString(Resource.String.search);

            searchView.QueryTextChange += (_, e) =>
            {
                var oldSearch = _authenticatorView.Search;

                _authenticatorView.Search = e.NewText;
                _authenticatorListAdapter.NotifyDataSetChanged();

                if (e.NewText == "")
                {
                    _authenticatorTouchHelperCallback.IsLocked = false;

                    if (!String.IsNullOrEmpty(oldSearch))
                    {
                        searchItem.CollapseActionView();
                    }
                }
                else
                {
                    _authenticatorTouchHelperCallback.IsLocked = true;
                }
            };

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnMenuOpened(int featureId, IMenu menu)
        {
            var sortItemId = _authenticatorView.SortMode switch
            {
                SortMode.AlphabeticalAscending => Resource.Id.actionSortAZ,
                SortMode.AlphabeticalDescending => Resource.Id.actionSortZA,
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
            _preferences.SortMode = sortMode;
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

            fragment.EditCategoriesClicked += delegate
            {
                _shouldLoadFromPersistenceOnNextOpen = true;
                StartActivity(typeof(EditCategoriesActivity));
            };

            fragment.SettingsClicked += delegate
            {
                StartActivityForResult(typeof(SettingsActivity), RequestSettingsRecreate);
            };

            fragment.AboutClicked += delegate
            {
                var sub = new AboutBottomSheet();

                sub.AboutClicked += delegate
                {
                    StartActivity(typeof(AboutActivity));
                };

                sub.SupportClicked += delegate
                {
                    StartWebBrowserActivity(Constants.BuyMeACoffee);
                };

                sub.RateClicked += delegate
                {
                    var intent = new Intent(Intent.ActionView, Uri.Parse("market://details?id=" + PackageName));

                    try
                    {
                        StartActivity(intent);
                    }
                    catch (ActivityNotFoundException)
                    {
                        Toast.MakeText(this, Resource.String.googlePlayNotInstalledError, ToastLength.Short).Show();
                    }
                };

                sub.ViewGitHubClicked += delegate
                {
                    StartWebBrowserActivity(Constants.GitHubRepo);
                };

                sub.Show(SupportFragmentManager, sub.Tag);
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnBackButtonPressed(object sender, EventArgs args)
        {
            var searchBarWasClosed = false;

            RunOnUiThread(delegate
            {
                var searchItem = _toolbar?.Menu.FindItem(Resource.Id.actionSearch);

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

            var defaultCategory = _preferences.DefaultCategory;

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

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        #endregion

        #region Database

        private async void OnUnlockAttempted(object sender, string password)
        {
            var fragment = (UnlockBottomSheet) sender;
            RunOnUiThread(delegate { fragment.SetBusy(); });

            try
            {
                await _database.Open(password, Database.Origin.Activity);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                RunOnUiThread(delegate { fragment.ShowError(); });
                return;
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

                RunOnUiThread(delegate
                {
                    AnimUtil.FadeOutView(_progressBar, AnimUtil.LengthShort, true);
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

            if (!_preventBackupReminder && _preferences.ShowBackupReminders &&
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

            builder.SetNeutralButton(Resource.String.viewErrorLog, delegate
            {
                var intent = new Intent(this, typeof(ErrorActivity));
                intent.PutExtra("exception", exception.ToString());
                StartActivity(intent);
            });

            builder.SetPositiveButton(Resource.String.retry, async delegate
            {
                await _database.Close(Database.Origin.Activity);
                Recreate();
            });

            builder.SetCancelable(false);
            builder.Create().Show();
        }

        #endregion

        #region Authenticator List

        private void InitViews()
        {
            _coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.coordinatorLayout);
            ViewCompat.SetOnApplyWindowInsetsListener(_coordinatorLayout!, this);

            _toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);

            if (_preferences.DefaultCategory == null)
            {
                SupportActionBar.SetTitle(Resource.String.categoryAll);
            }
            else
            {
                SupportActionBar.SetDisplayShowTitleEnabled(false);
            }

            _appBarLayout = FindViewById<AppBarLayout>(Resource.Id.appBarLayout);
            _bottomAppBar = FindViewById<BottomAppBar>(Resource.Id.bottomAppBar);
            _bottomAppBar.NavigationClick += OnBottomAppBarNavigationClick;
            _bottomAppBar.MenuItemClick += delegate
            {
                if (_authenticatorListAdapter == null)
                {
                    return;
                }

                _toolbar.Menu.FindItem(Resource.Id.actionSearch).ExpandActionView();
                ScrollToPosition(0);
            };

            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddButtonClick;

            _authenticatorList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);
            _emptyMessageText = FindViewById<TextView>(Resource.Id.textEmptyMessage);

            _startLayout = FindViewById<LinearLayout>(Resource.Id.layoutStart);

            var viewGuideButton = FindViewById<MaterialButton>(Resource.Id.buttonViewGuide);
            viewGuideButton.Click += delegate { StartActivity(typeof(GuideActivity)); };

            var importButton = FindViewById<MaterialButton>(Resource.Id.buttonImport);
            importButton.Click += delegate { OpenImportMenu(); };
        }

        private void InitAuthenticatorList()
        {
            _authenticatorListAdapter =
                new AuthenticatorListAdapter(this, _authenticatorService, _authenticatorView, _customIconRepository,
                    IsDark) { HasStableIds = true };

            _authenticatorListAdapter.ItemClicked += OnAuthenticatorClicked;
            _authenticatorListAdapter.MenuClicked += OnAuthenticatorMenuClicked;
            _authenticatorListAdapter.MovementStarted += OnAuthenticatorListMovementStarted;
            _authenticatorListAdapter.MovementFinished += OnAuthenticatorListMovementFinished;

            _authenticatorList.SetAdapter(_authenticatorListAdapter);

            var viewMode = ViewModeSpecification.FromName(_preferences.ViewMode);
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
                await _authenticatorCategoryService.UpdateManyAsync(authenticatorCategories);
            }

            if (_preferences.SortMode != SortMode.Custom)
            {
                _preferences.SortMode = SortMode.Custom;
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

            var category = await _categoryRepository.GetAsync(_authenticatorView.CategoryId);

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
                var category = await _categoryRepository.GetAsync(id);
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

            _appBarLayout.SetExpanded(true);
        }

        private void OnAuthenticatorClicked(object sender, int position)
        {
            var auth = _authenticatorView[position];
            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("code", auth.GetCode());
            clipboard.PrimaryClip = clip;

            ShowSnackbar(Resource.String.copiedToClipboard, Snackbar.LengthShort);
        }

        private void OnAuthenticatorMenuClicked(object sender, int position)
        {
            var auth = _authenticatorView[position];
            var bundle = new Bundle();
            bundle.PutInt("type", (int) auth.Type);
            bundle.PutLong("counter", auth.Counter);

            var fragment = new AuthenticatorMenuBottomSheet { Arguments = bundle };

            fragment.RenameClicked += delegate { OpenRenameDialog(position); };
            fragment.ChangeIconClicked += delegate { OpenIconDialog(position); };
            fragment.AssignCategoriesClicked += async delegate { await OpenCategoriesDialog(position); };
            fragment.ShowQrCodeClicked += delegate { OpenQrCodeDialog(position); };
            fragment.DeleteClicked += delegate { OpenDeleteDialog(position); };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenQrCodeDialog(int position)
        {
            var auth = _authenticatorView[position];
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

        private void OpenDeleteDialog(int position)
        {
            var auth = _authenticatorView[position];

            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                try
                {
                    await _authenticatorService.DeleteWithCategoryBindingsAsync(auth);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                try
                {
                    await _customIconService.CullUnused();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    // ignored
                }

                await _authenticatorView.LoadFromPersistenceAsync();
                RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemRemoved(position); });
                CheckEmptyState();

                _preferences.BackupRequired = BackupRequirement.WhenPossible;
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
                    subFragment.FromGalleryClicked += delegate { StartFilePickActivity("image/*", RequestQrCodeFromImage); };
                    subFragment.Show(SupportFragmentManager, subFragment.Tag);
                }
                else
                {
                    StartFilePickActivity("image/*", RequestQrCodeFromImage);
                }
            };

            fragment.EnterKeyClicked += OpenAddDialog;
            fragment.RestoreClicked += delegate
            {
                StartFilePickActivity(Backup.MimeType, RequestRestore);
            };

            fragment.ImportClicked += delegate
            {
                OpenImportMenu();
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        #endregion

        #region QR Code Scanning

#if FDROID
        private async Task ScanQrCodeFromImage(Uri uri)
        {
            Bitmap bitmap;

            try
            {
                var data = await FileUtil.ReadFile(this, uri);
                bitmap = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            if (bitmap == null)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            var reader = new BarcodeReader<Bitmap>(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
                    TryHarder = true,
                    TryInverted = true
                }
            };

            ZXing.Result result;

            try
            {
                var buffer = ByteBuffer.Allocate(bitmap.ByteCount);
                await bitmap.CopyPixelsToBufferAsync(buffer);
                buffer.Rewind();

                var bytes = new byte[buffer.Remaining()];
                buffer.Get(bytes);

                var source = new RGBLuminanceSource(bytes, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGBA32);
                result = await Task.Run(() => reader.Decode(source));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if (result == null)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            await ParseQrCodeScanResult(result.Text);
        }
#else
        private async Task ScanQrCodeFromImage(Uri uri)
        {
            InputImage image;

            try
            {
                image = await Task.Run(() => InputImage.FromFilePath(this, uri));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            var options = new BarcodeScannerOptions.Builder()
                .SetBarcodeFormats(Barcode.FormatQrCode)
                .Build();

            var scanner = BarcodeScanning.GetClient(options);
            JavaList<Barcode> barcodes;

            try
            {
                barcodes = await scanner.Process(image).AsAsync<JavaList<Barcode>>();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if (!barcodes.Any())
            {
                ShowSnackbar(Resource.String.qrCodeNotFound, Snackbar.LengthShort);
                return;
            }

            foreach (var barcode in barcodes)
            {
                await ParseQrCodeScanResult(barcode.RawValue);
            }
        }
#endif

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

            _preferences.BackupRequired = BackupRequirement.Urgent;
        }

        private async Task OnUriScan(string uri)
        {
            UriParseResult result;

            try
            {
                result = Authenticator.ParseUri(uri, _iconResolver);
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
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                if (_authenticatorView.CategoryId != null)
                {
                    var category = await _categoryRepository.GetAsync(_authenticatorView.CategoryId);
                    await _authenticatorCategoryService.AddAsync(result.Authenticator, category);
                }

                await _authenticatorView.LoadFromPersistenceAsync();
                CheckEmptyState();

                var position = _authenticatorView.IndexOf(result.Authenticator);

                RunOnUiThread(delegate
                {
                    _authenticatorListAdapter.NotifyItemInserted(position);
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

        private async Task OnOtpAuthMigrationScan(string uri)
        {
            int added;

            try
            {
                added = await _qrCodeService.ParseOtpMigrationUri(uri);
            }
            catch (ArgumentException)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();
            await SwitchCategory(null);
            RunOnUiThread(_authenticatorListAdapter.NotifyDataSetChanged);

            var message = String.Format(GetString(Resource.String.restoredFromMigration), added);
            ShowSnackbar(message, Snackbar.LengthLong);
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
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Google-Authenticator");
            };

            // Use */* mime-type for most binary files because some files might not show on older Android versions
            // Use */* for json also, because application/json doesn't work

            fragment.AuthenticatorPlusClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportAuthenticatorPlus);
            };

            fragment.AndOtpClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportAndOtp);
            };

            fragment.FreeOtpPlusClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportFreeOtpPlus);
            };

            fragment.AegisClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportAegis);
            };

            fragment.BitwardenClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportBitwarden);
            };

            fragment.WinAuthClicked += delegate
            {
                StartFilePickActivity("text/plain", RequestImportWinAuth);
            };

            fragment.TwoFasClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportTwoFas);
            };

            fragment.AuthyClicked += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Authy");
            };

            fragment.TotpAuthenticatorClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportTotpAuthenticator);
            };

            fragment.BlizzardAuthenticatorClicked += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Blizzard-Authenticator");
            };

            fragment.SteamClicked += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Steam");
            };

            fragment.UriListClicked += delegate
            {
                StartFilePickActivity("*/*", RequestImportUriList);
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task RestoreFromUri(Uri uri)
        {
            byte[] data;

            try
            {
                data = await FileUtil.ReadFile(this, uri);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            if (data.Length == 0)
            {
                ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                return;
            }

            async Task<RestoreResult> DecryptAndRestore(string password)
            {
                var backup = await Task.Run(() => Backup.FromBytes(data, password));
                return await _restoreService.RestoreAndUpdateAsync(backup);
            }

            if (Backup.IsReadableWithoutPassword(data))
            {
                RestoreResult result;
                SetLoading(true);

                try
                {
                    result = await DecryptAndRestore(null);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                    return;
                }
                finally
                {
                    SetLoading(false);
                }

                await FinaliseRestore(result);
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
            var sheet = new BackupPasswordBottomSheet { Arguments = bundle };

            sheet.PasswordEntered += async (_, password) =>
            {
                sheet.SetBusyText(Resource.String.decrypting);

                try
                {
                    var result = await DecryptAndRestore(password);
                    sheet.Dismiss();
                    await FinaliseRestore(result);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    sheet.Error = GetString(Resource.String.restoreError);
                    sheet.SetBusyText(null);
                }
            };

            sheet.Show(SupportFragmentManager, sheet.Tag);
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
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            async Task ConvertAndRestore(string password)
            {
                var (conversionResult, restoreResult) = await _importService.ImportAsync(converter, data, password);

                foreach (var failure in conversionResult.Failures)
                {
                    var message = String.Format(
                        GetString(Resource.String.importConversionError), failure.Description, failure.Error);

                    new MaterialAlertDialogBuilder(this)
                        .SetTitle(Resource.String.importIncomplete)
                        .SetMessage(message)
                        .SetPositiveButton(Resource.String.ok, delegate { })
                        .Show();
                }

                await FinaliseRestore(restoreResult);
                _preferences.BackupRequired = BackupRequirement.Urgent;
            }

            void ShowPasswordSheet()
            {
                var bundle = new Bundle();
                bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
                var sheet = new BackupPasswordBottomSheet { Arguments = bundle };

                sheet.PasswordEntered += async (_, password) =>
                {
                    sheet.SetBusyText(Resource.String.decrypting);

                    try
                    {
                        await ConvertAndRestore(password);
                        sheet.Dismiss();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        sheet.Error = GetString(Resource.String.restoreError);
                        sheet.SetBusyText(null);
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
                        Logger.Error(e);
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

        private async Task FinaliseRestore(RestoreResult result)
        {
            ShowSnackbar(result.ToString(this), Snackbar.LengthShort);

            if (result.IsVoid())
            {
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();
            await SwitchCategory(null);

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

            fragment.BackupFileClicked += delegate
            {
                ShowPicker("*/*", RequestBackupFile, Backup.FileExtension);
            };

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

                try
                {
                    var data = await Task.Run(() => backup.ToBytes(password));
                    await FileUtil.WriteFile(this, destination, data);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                FinaliseBackup();
            }

            if (_preferences.PasswordProtected && _preferences.DatabasePasswordBackup)
            {
                var password = await SecureStorageWrapper.GetDatabasePassword();
                await DoBackup(password);
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Set);
            var fragment = new BackupPasswordBottomSheet { Arguments = bundle };

            fragment.PasswordEntered += async (sender, password) =>
            {
                var busyText = !String.IsNullOrEmpty(password) ? Resource.String.encrypting : Resource.String.saving;
                fragment.SetBusyText(busyText);
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
                Logger.Error(e);
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
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            FinaliseBackup();
        }

        private void FinaliseBackup()
        {
            _preferences.BackupRequired = BackupRequirement.NotRequired;
            ShowSnackbar(Resource.String.saveSuccess, Snackbar.LengthLong);
        }

        private void RemindBackup()
        {
            if (!_authenticatorView.AnyWithoutFilter())
            {
                return;
            }

            if (_preferences.BackupRequired != BackupRequirement.Urgent || _preferences.AutoBackupEnabled)
            {
                return;
            }

            _lastBackupReminderTime = DateTime.UtcNow;
            var snackbar = Snackbar.Make(_coordinatorLayout, Resource.String.backupReminder, Snackbar.LengthLong);
            snackbar.SetAnchorView(_addButton);
            snackbar.SetAction(Resource.String.backupNow, delegate
            {
                OpenBackupMenu();
            });

            var callback = new SnackbarCallback();
            callback.Dismissed += (_, e) =>
            {
                if (e == Snackbar.Callback.DismissEventSwipe)
                {
                    _preferences.BackupRequired = BackupRequirement.NotRequired;
                }
            };

            snackbar.AddCallback(callback);
            snackbar.Show();
        }

        #endregion

        #region Add Dialog

        private void OpenAddDialog(object sender, EventArgs e)
        {
            var fragment = new AddAuthenticatorBottomSheet();
            fragment.AddClicked += OnAddDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAddDialogSubmit(object sender, Authenticator auth)
        {
            var dialog = (AddAuthenticatorBottomSheet) sender;

            try
            {
                if (_authenticatorView.CategoryId == null)
                {
                    await _authenticatorService.AddAsync(auth);
                }
                else
                {
                    await _authenticatorService.AddAsync(auth);

                    var category = await _categoryRepository.GetAsync(_authenticatorView.CategoryId);
                    await _authenticatorCategoryService.AddAsync(auth, category);
                }
            }
            catch (EntityDuplicateException)
            {
                dialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            await _authenticatorView.LoadFromPersistenceAsync();
            CheckEmptyState();

            var position = _authenticatorView.IndexOf(auth);

            RunOnUiThread(delegate
            {
                _authenticatorListAdapter.NotifyItemInserted(position);
                ScrollToPosition(position);
            });

            dialog.Dismiss();
            _preferences.BackupRequired = BackupRequirement.Urgent;
        }

        #endregion

        #region Rename Dialog

        private void OpenRenameDialog(int position)
        {
            var auth = _authenticatorView[position];

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutString("issuer", auth.Issuer);
            bundle.PutString("username", auth.Username);

            var fragment = new RenameAuthenticatorBottomSheet { Arguments = bundle };
            fragment.RenameClicked += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, RenameAuthenticatorBottomSheet.RenameEventArgs args)
        {
            var auth = _authenticatorView[args.ItemPosition];

            try
            {
                await _authenticatorService.RenameAsync(auth, args.Issuer, args.Username);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(args.ItemPosition); });
            _preferences.BackupRequired = BackupRequirement.WhenPossible;
        }

        #endregion

        #region Icon Dialog

        private void OpenIconDialog(int position)
        {
            var bundle = new Bundle();
            bundle.PutInt("position", position);

            var fragment = new ChangeIconBottomSheet { Arguments = bundle };
            fragment.IconSelected += OnIconDialogIconSelected;
            fragment.UseCustomIconClick += delegate
            {
                _customIconApplyPosition = position;
                StartFilePickActivity("image/*", RequestCustomIcon);
            };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnIconDialogIconSelected(object sender, ChangeIconBottomSheet.IconSelectedEventArgs args)
        {
            var auth = _authenticatorView[args.ItemPosition];
            var oldIcon = auth.Icon;

            try
            {
                await _authenticatorService.SetIconAsync(auth, args.Icon);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            _preferences.BackupRequired = BackupRequirement.WhenPossible;
            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(args.ItemPosition); });

            ((ChangeIconBottomSheet) sender).Dismiss();
        }

        #endregion

        #region Custom Icons

        private async Task SetCustomIcon(Uri source, int position)
        {
            SetLoading(true);

            CustomIcon icon;

            try
            {
                var data = await FileUtil.ReadFile(this, source);
                icon = await _customIconDecoder.Decode(data);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            finally
            {
                SetLoading(false);
            }

            var auth = _authenticatorView[position];
            var oldIcon = auth.Icon;

            try
            {
                await _authenticatorService.SetCustomIconAsync(auth, icon);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            _preferences.BackupRequired = BackupRequirement.WhenPossible;

            RunOnUiThread(delegate { _authenticatorListAdapter.NotifyItemChanged(position); });
        }

        #endregion

        #region Categories

        private async Task OpenCategoriesDialog(int position)
        {
            var auth = _authenticatorView[position];
            var authenticatorCategories = await _authenticatorCategoryRepository.GetAllForAuthenticatorAsync(auth);
            var categoryIds = authenticatorCategories.Select(ac => ac.CategoryId).ToArray();

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutStringArray("assignedCategoryIds", categoryIds);

            var fragment = new AssignCategoriesBottomSheet { Arguments = bundle };
            fragment.CategoryClicked += OnCategoriesDialogCategoryClicked;
            fragment.EditCategoriesClicked += delegate
            {
                _shouldLoadFromPersistenceOnNextOpen = true;
                StartActivity(typeof(EditCategoriesActivity));
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

            RunOnUiThread(delegate
            {
                _authenticatorListAdapter.NotifyDataSetChanged();
            });

            CheckEmptyState();
        }

        private async void OnCategoriesDialogCategoryClicked(object sender,
            AssignCategoriesBottomSheet.CategoryClickedEventArgs args)
        {
            var auth = _authenticatorView[args.ItemPosition];
            var category = await _categoryRepository.GetAsync(args.CategoryId);

            try
            {
                if (args.IsChecked)
                {
                    await _authenticatorCategoryService.AddAsync(auth, category);
                }
                else
                {
                    await _authenticatorCategoryService.RemoveAsync(auth, category);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
            }
        }

        #endregion

        #region Misc

        private void ShowSnackbar(int textRes, int length)
        {
            var snackbar = Snackbar.Make(_coordinatorLayout, textRes, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }

        private void ShowSnackbar(string message, int length)
        {
            var snackbar = Snackbar.Make(_coordinatorLayout, message, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }

        private void SetLoading(bool loading)
        {
            RunOnUiThread(delegate
            {
                _progressBar.Visibility = loading ? ViewStates.Visible : ViewStates.Invisible;
            });
        }

        private void StartFilePickActivity(string mimeType, int requestCode)
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);

            BaseApplication.PreventNextAutoLock = true;

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong);
            }
        }

        private void StartFileSaveActivity(string mimeType, int requestCode, string fileName)
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);
            intent.PutExtra(Intent.ExtraTitle, fileName);

            BaseApplication.PreventNextAutoLock = true;

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong);
            }
        }

        private void StartWebBrowserActivity(string url)
        {
            var intent = new Intent(Intent.ActionView, Uri.Parse(url));

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.webBrowserMissing, Snackbar.LengthLong);
            }
        }

        private void TriggerAutoBackupWorker()
        {
            if (!_preferences.AutoBackupEnabled && !_preferences.AutoRestoreEnabled)
            {
                return;
            }

            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            var manager = WorkManager.GetInstance(this);
            manager.EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace!, request);
        }

        #endregion
    }
}