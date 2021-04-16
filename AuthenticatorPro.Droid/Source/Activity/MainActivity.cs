using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
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
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Data.Backup;
using AuthenticatorPro.Droid.Data.Source;
using AuthenticatorPro.Droid.Fragment;
using AuthenticatorPro.Droid.List;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Backup;
using AuthenticatorPro.Shared.Data.Backup.Converter;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Internal;
using Google.Android.Material.Snackbar;
using Java.Nio;
using SQLite;
using ZXing;
using ZXing.Common;
using ZXing.Mobile;
using IResult = AuthenticatorPro.Droid.Data.Backup.IResult;
using Result = Android.App.Result;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using Timer = System.Timers.Timer;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/MainActivityTheme", MainLauncher = true,
              Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustPan,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    [IntentFilter (new[] { Intent.ActionView }, Categories = new[] {
        Intent.CategoryDefault,
        Intent.CategoryBrowsable
    }, DataSchemes = new[] { "otpauth", "otpauth-migration" })]
    internal class MainActivity : BaseActivity, CapabilityClient.IOnCapabilityChangedListener, IOnApplyWindowInsetsListener
    {
        private const string WearRefreshCapability = "refresh";
        private const int PermissionCameraCode = 0;

        private const int BackupReminderThresholdMinutes = 120;
        private const int ListPaddingBottom = 80;

        // Request codes
        private const int RequestUnlock = 0;
        private const int RequestRestore = 1;
        private const int RequestBackupFile = 2;
        private const int RequestBackupHtml = 3;
        private const int RequestBackupUriList = 4;
        private const int RequestQrCode = 5;
        private const int RequestCustomIcon = 6;
        private const int RequestSettingsRecreate = 7;
        private const int RequestImportAuthenticatorPlus = 8;
        private const int RequestImportAndOtp = 9;
        private const int RequestImportFreeOtpPlus = 10;
        private const int RequestImportAegis = 11;
        private const int RequestImportBitwarden = 12;
        private const int RequestImportWinAuth = 13;
        private const int RequestImportTotpAuthenticator = 14;
        private const int RequestImportUriList = 15;

        // Views
        private CoordinatorLayout _coordinatorLayout;
        private AppBarLayout _appBarLayout;
        private MaterialToolbar _toolbar;
        private ProgressBar _progressBar;
        private RecyclerView _authList;
        private FloatingActionButton _addButton;
        private BottomAppBar _bottomAppBar;

        private LinearLayout _emptyStateLayout;
        private TextView _emptyMessageText;
        private LinearLayout _startLayout;

        private AuthenticatorListAdapter _authListAdapter;
        private AutoGridLayoutManager _authLayout;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;

        // State
        private readonly IconResolver _iconResolver;
        private PreferenceWrapper _preferences;
        private Timer _timer;
        private DateTime _pauseTime;
        private DateTime _lastBackupReminderTime;
        
        private bool _preventBackupReminder;
        private bool _justLaunched;
        private bool _updateOnActivityResume;
        private bool _currentlyResuming;
        private int _customIconApplyPosition;

        // Wear OS State
        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        // Activity lifecycle synchronisation
        // Async activity lifecycle methods pass control to the next method, Resume is called before Create has finished.
        // Hand control to the next method when it's safe to do so.
        private readonly SemaphoreSlim _onCreateLock;
        private readonly SemaphoreSlim _onResumeLock;
        // Pause OnCreate until unlock is complete
        private readonly SemaphoreSlim _unlockDatabaseLock;


        public MainActivity()
        {   
            _iconResolver = new IconResolver();
            _onCreateLock = new SemaphoreSlim(1, 1);
            _onResumeLock = new SemaphoreSlim(1, 1);
            _unlockDatabaseLock = new SemaphoreSlim(0, 1);
        }

        #region Activity Lifecycle
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            await _onCreateLock.WaitAsync();

            _justLaunched = true;
            
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            _preferences = new PreferenceWrapper(this);
            
            SetContentView(Resource.Layout.activityMain);
            var windowFlags = WindowManagerFlags.Secure;

            // BuildVersionCodes.R == 10000?, use fixed value for now
            if((int) Build.VERSION.SdkInt >= 30)
            {
                Window.SetDecorFitsSystemWindows(false);
                Window.SetStatusBarColor(Color.Transparent);
                Window.SetNavigationBarColor(Color.Transparent);
                
                if(!IsDark)
                    Window.InsetsController?.SetSystemBarsAppearance(
                        (int) WindowInsetsControllerAppearance.LightStatusBars, (int) WindowInsetsControllerAppearance.LightStatusBars);
            }
            else
                windowFlags |= WindowManagerFlags.TranslucentStatus;
            
            Window.SetFlags(windowFlags, windowFlags);
            InitViews();

            if(savedInstanceState != null)
            {
                _pauseTime = new DateTime(savedInstanceState.GetLong("pauseTime"));
                _lastBackupReminderTime = new DateTime(savedInstanceState.GetLong("lastBackupReminderTime"));
            }
            else
            {
                _pauseTime = DateTime.MinValue;
                _lastBackupReminderTime = DateTime.MinValue;
            }

            await Database.UpgradeLegacy(this);
            SQLiteAsyncConnection connection;

            try
            {
                connection = await GetConnection();
            }
            catch(InvalidOperationException)
            {
                Logger.Error("Shared connection not open after unlock");
                throw;
            }
            catch(Exception e)
            {
                Logger.Error(e);
                ShowDatabaseErrorDialog();
                return;
            }

            _categorySource = new CategorySource(connection);
            _customIconSource = new CustomIconSource(connection);
            _authSource = new AuthenticatorSource(connection);

            if(_preferences.DefaultCategory != null)
                _authSource.SetCategory(_preferences.DefaultCategory);
            
            RunOnUiThread(InitAuthenticatorList);

            _timer = new Timer {
                Interval = 1000,
                AutoReset = true
            };
            
            _timer.Elapsed += delegate
            {
                Tick();
            };

            _updateOnActivityResume = true;
            _onCreateLock.Release();
            
            if(_preferences.FirstLaunch)
                StartActivity(typeof(IntroActivity));

            DetectGoogleAPIsAvailability();
            await DetectWearOSCapability();

            // Handle QR code scanning from intent
            if(Intent.Data == null)
                return;
            
            await _onResumeLock.WaitAsync();
            _onResumeLock.Release();
                
            var uri = Intent.Data;
            await ParseQRCodeScanResult(uri.ToString());
        }

        protected override async void OnResume()
        {
            base.OnResume();
           
            // Prevent double calls to onresume when unlocking database
            if(_currentlyResuming)
                return;

            _currentlyResuming = true;
            
            await _onCreateLock.WaitAsync();
            _onCreateLock.Release();
            
            await _onResumeLock.WaitAsync();
            
            RunOnUiThread(delegate
            {
                // Perhaps the animation in onpause was cancelled
                _authList.Visibility = ViewStates.Invisible;
            });

            try
            {
                await UnlockIfRequired();
            }
            catch(Exception e)
            {
                Logger.Error($"Database not usable? error: {e}");
                ShowDatabaseErrorDialog();
                return;
            }

            // In case auto restore occurs when activity is loaded
            var autoRestoreCompleted = _preferences.AutoRestoreCompleted;
            _preferences.AutoRestoreCompleted = false;

            if(_updateOnActivityResume || _justLaunched || autoRestoreCompleted)
            {
                _updateOnActivityResume = false;
                await Update(_justLaunched);

                if(_authSource.CategoryId != null)
                {
                    // Currently visible category has been deleted
                    if(_categorySource.GetView().All(c => c.Id != _authSource.CategoryId))
                        SwitchCategory(null);
                    else
                    {
                        var category = _categorySource.GetAll().FirstOrDefault(c => c.Id == _authSource.CategoryId);

                        if(category != null)
                        {
                            RunOnUiThread(delegate
                            {
                                SupportActionBar.SetDisplayShowTitleEnabled(true);
                                SupportActionBar.Title = category.Name;
                            });
                        }
                    }
                }
            }
            else
                Tick();
            
            _justLaunched = false;
            _currentlyResuming = false;
            
            _onResumeLock.Release();
            CheckEmptyState();

            if(!_preventBackupReminder && _preferences.ShowBackupReminders && (DateTime.UtcNow - _lastBackupReminderTime).TotalMinutes > BackupReminderThresholdMinutes)
                RemindBackup();

            _preventBackupReminder = false;
            
            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).AddListenerAsync(this, WearRefreshCapability);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutLong("pauseTime", _pauseTime.Ticks);
            outState.PutLong("lastBackupReminderTime", _lastBackupReminderTime.Ticks);
        }
        
        protected override async void OnPause()
        {
            base.OnPause();

            _timer?.Stop();
            _pauseTime = DateTime.UtcNow;
            
            RunOnUiThread(delegate
            {
                if(_authList != null)
                    AnimUtil.FadeOutView(_authList, AnimUtil.LengthLong);
            });

            if(!_hasWearAPIs)
                return;
            
            try
            {
                await WearableClass.GetCapabilityClient(this).RemoveListenerAsync(this, WearRefreshCapability);
            }
            catch(ApiException) { }
        }
        #endregion

        #region Activity Events
        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);
            _preventBackupReminder = true;

            if(requestCode == RequestUnlock)
            {
                if(resultCode != Result.Ok)
                {
                    FinishAffinity();
                    return;
                }

                _unlockDatabaseLock.Release();
                return;
            }
            
            if(resultCode != Result.Ok)
                return;

            if(requestCode == RequestSettingsRecreate)
            {
                await _onResumeLock.WaitAsync();
                Recreate();
                return;
            }

            await _onResumeLock.WaitAsync();
            _onResumeLock.Release();
            
            switch(requestCode)
            {
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
                
                case RequestQrCode:
                    await ScanQRCodeFromImage(intent.Data);
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
                    await ImportFromUri(new AegisBackupConverter(_iconResolver, new CustomIconDecoder()), intent.Data);
                    break;
                
                case RequestImportBitwarden:
                    await ImportFromUri(new BitwardenBackupConverter(_iconResolver), intent.Data);
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
                RunOnUiThread(_authListAdapter.NotifyDataSetChanged);
            });
        }

        public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insets)
        {
            var layout = FindViewById<LinearLayout>(Resource.Id.toolbarWrapLayout);
            layout.SetPadding(0, insets.SystemWindowInsetTop, 0, 0);

            var bottomPadding = (int) ViewUtils.DpToPx(this, ListPaddingBottom) + insets.SystemWindowInsetBottom;
            _authList.SetPadding(0, 0, 0, bottomPadding);
            
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
                var oldSearch = _authSource.Search;

                _authSource.SetSearch(e.NewText);
                _authListAdapter.NotifyDataSetChanged();

                if(e.NewText == "" && !String.IsNullOrEmpty(oldSearch))
                    searchItem.CollapseActionView();
            };

            searchView.Close += delegate
            {
                searchItem.CollapseActionView();
                _authSource.SetSearch(null);
            };

            return base.OnCreateOptionsMenu(menu);
        }

        private void OnBottomAppBarNavigationClick(object sender, Toolbar.NavigationClickEventArgs e)
        {
            if(_authSource == null || _categorySource == null)
                return;

            var categoryIds = _categorySource.GetView().Select(c => c.Id).ToArray();
            var categoryNames = _categorySource.GetView().Select(c => c.Name).ToArray();
            
            var bundle = new Bundle();
            bundle.PutStringArray("categoryIds", categoryIds);
            bundle.PutStringArray("categoryNames", categoryNames);
            bundle.PutString("currentCategoryId", _authSource.CategoryId);
            
            var fragment = new MainMenuBottomSheet {Arguments = bundle};
            fragment.ClickCategory += (_, id) =>
            {
                SwitchCategory(id);
                RunOnUiThread(fragment.Dismiss);
            };

            fragment.ClickBackup += delegate
            {
                if(!_authSource.GetAll().Any())
                {
                    ShowSnackbar(Resource.String.noAuthenticators, Snackbar.LengthShort);
                    return;
                }

                OpenBackupMenu();
            };

            fragment.ClickManageCategories += delegate
            {
                _updateOnActivityResume = true;
                StartActivity(typeof(ManageCategoriesActivity));
            };
            
            fragment.ClickSettings += delegate
            {
                StartActivityForResult(typeof(SettingsActivity), RequestSettingsRecreate);
            };
            
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        public override async void OnBackPressed()
        {
            var searchBarWasClosed = false;
            
            RunOnUiThread(delegate
            {
                var searchItem = _toolbar?.Menu.FindItem(Resource.Id.actionSearch);

                if(searchItem == null || !searchItem.IsActionViewExpanded)
                    return;
                
                searchItem.CollapseActionView();
                searchBarWasClosed = true;
            });

            if(searchBarWasClosed)
                return;

            var defaultCategory = _preferences.DefaultCategory;

            if(defaultCategory == null)
            {
                if(_authSource?.CategoryId != null)
                {
                    SwitchCategory(null);
                    return;
                }
            }
            else
            {
                if(_authSource?.CategoryId != defaultCategory)
                {
                    SwitchCategory(defaultCategory);
                    return;
                }
            }

            await BaseApplication.Lock();
            Finish();
        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionCameraCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    await ScanQRCodeFromCamera();
                else
                    ShowSnackbar(Resource.String.cameraPermissionError, Snackbar.LengthShort);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        #endregion
        
        #region Database
        private async Task<SQLiteAsyncConnection> GetConnection()
        {
            var tries = 0;

            while(true)
            {
                await UnlockIfRequired();

                try
                {
                    var connection = await Database.GetSharedConnection();
                    return connection;
                }
                catch
                {
                    // Try again, busy perhaps?
                    if(++tries == 3)
                        throw;
                }
            }
        }
        
        private async Task UnlockIfRequired()
        {
            switch(BaseApplication.IsLocked)
            {
                // Unlocked, no need to do anything
                case false:
                    return;
                
                // Locked and has password, wait for unlock in unlockactivity
                case true when _preferences.PasswordProtected:
                    StartActivityForResult(typeof(UnlockActivity), RequestUnlock);
                    await _unlockDatabaseLock.WaitAsync();
                    break;
                    
                // Locked but no password, unlock now
                case true:
                    await BaseApplication.Unlock(null);
                    break;
            }
        }
        
        private void ShowDatabaseErrorDialog()
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.databaseError);
            builder.SetTitle(Resource.String.error);
            builder.SetPositiveButton(Resource.String.quit, delegate
            {
                Process.KillProcess(Process.MyPid());
            });
            builder.SetCancelable(false);

            var dialog = builder.Create();
            dialog.Show();
        }
        #endregion

        #region Authenticator List
        private void InitViews()
        {
            _coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.coordinatorLayout);
            ViewCompat.SetOnApplyWindowInsetsListener(_coordinatorLayout, this);
            
            _toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);
            
            if(_preferences.DefaultCategory == null)
                SupportActionBar.SetTitle(Resource.String.categoryAll);
            else
                SupportActionBar.SetDisplayShowTitleEnabled(false);

            _appBarLayout = FindViewById<AppBarLayout>(Resource.Id.appBarLayout);
            _bottomAppBar = FindViewById<BottomAppBar>(Resource.Id.bottomAppBar);
            _bottomAppBar.NavigationClick += OnBottomAppBarNavigationClick;
            _bottomAppBar.MenuItemClick += delegate
            {
                if(_authSource == null || _authListAdapter == null)
                    return;
                
                _toolbar.Menu.FindItem(Resource.Id.actionSearch).ExpandActionView();
                ScrollToPosition(0);
            };
            
            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddButtonClick;

            _authList = FindViewById<RecyclerView>(Resource.Id.list);
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
            var viewMode = ViewModeSpecification.FromName(_preferences.ViewMode);
            _authListAdapter = new AuthenticatorListAdapter(this, _authSource, _customIconSource, viewMode, IsDark)
            {
                HasStableIds = true
            };

            _authListAdapter.ItemClick += OnAuthenticatorClick;
            _authListAdapter.MenuClick += OnAuthenticatorOptionsClick;
            _authListAdapter.MovementStarted += delegate
            {
                _bottomAppBar.PerformHide();
            };
            
            _authListAdapter.MovementFinished += async delegate
            {
                RunOnUiThread(_bottomAppBar.PerformShow);
                await NotifyWearAppOfChange();
            };

            _authList.SetAdapter(_authListAdapter);

            _authLayout = new AutoGridLayoutManager(this, viewMode.GetMinColumnWidth());
            _authList.SetLayoutManager(_authLayout);

            _authList.AddItemDecoration(new GridSpacingItemDecoration(this, _authLayout, viewMode.GetSpacing(), true));
            _authList.HasFixedSize = false;

            var animation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            var callback = new ReorderableListTouchHelperCallback(this, _authListAdapter, _authLayout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);
        }

        private async Task Update(bool animateLayout)
        {
            var uiLock = new SemaphoreSlim(0, 1);
            var showingProgress = false;
            
            var loadTimer = new Timer(400)
            {
                Enabled = false, AutoReset = false
            };

            loadTimer.Elapsed += delegate
            {
                RunOnUiThread(delegate
                {
                    showingProgress = true;
                    
                    AnimUtil.FadeInView(_progressBar, AnimUtil.LengthShort, false, delegate
                    {
                        if(uiLock.CurrentCount == 0)
                            uiLock.Release();
                    });
                });
            };

            var alreadyLoading = _progressBar.Visibility == ViewStates.Visible;

            if(!alreadyLoading)
                loadTimer.Enabled = true;

            await _authSource.Update();
            await _customIconSource.Update();
            await _categorySource.Update();

            loadTimer.Enabled = false;

            if(showingProgress)
                await uiLock.WaitAsync();

            RunOnUiThread(delegate
            {
                _authListAdapter.NotifyDataSetChanged();
                Tick();
                
                if(animateLayout)
                    _authList.ScheduleLayoutAnimation();
                
                if(showingProgress || alreadyLoading)
                    AnimUtil.FadeOutView(_progressBar, AnimUtil.LengthShort, true);
                    
                AnimUtil.FadeInView(_authList, AnimUtil.LengthShort, true, delegate
                {
                    uiLock.Release();
                });
            });

            await uiLock.WaitAsync();
        }

        private void CheckEmptyState()
        {
            if(!_authSource.GetView().Any())
            {
                RunOnUiThread(delegate
                {
                    if(_emptyStateLayout.Visibility == ViewStates.Invisible)
                        AnimUtil.FadeInView(_emptyStateLayout, AnimUtil.LengthLong);
                    
                    if(_authList.Visibility == ViewStates.Visible)
                        AnimUtil.FadeOutView(_authList, AnimUtil.LengthShort); 
                    
                    if(_authSource.CategoryId == null)
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
                    if(_emptyStateLayout.Visibility == ViewStates.Visible)
                        AnimUtil.FadeOutView(_emptyStateLayout, AnimUtil.LengthShort);
                    
                    if(_authList.Visibility == ViewStates.Invisible)
                        AnimUtil.FadeInView(_authList, AnimUtil.LengthLong);
                    
                    var firstVisiblePos = _authLayout.FindFirstCompletelyVisibleItemPosition();
                    var lastVisiblePos = _authLayout.FindLastCompletelyVisibleItemPosition();
                    
                    var shouldShowOverscroll =
                        firstVisiblePos >= 0 && lastVisiblePos >= 0 &&
                        (firstVisiblePos > 0 || lastVisiblePos < _authSource.GetView().Count - 1);
                    
                    _authList.OverScrollMode = shouldShowOverscroll ? OverScrollMode.Always : OverScrollMode.Never;
                    
                    if(!shouldShowOverscroll)
                        ScrollToPosition(0);
                });
                
                _timer.Start();
            }
        }

        private void SwitchCategory(string id)
        {
            if(id == _authSource.CategoryId)
            {
                CheckEmptyState();
                return;
            }

            string categoryName;
           
            if(id == null)
            {
                _authSource.SetCategory(null);
                categoryName = GetString(Resource.String.categoryAll);
            }
            else
            {
                var category = _categorySource.GetView().First(c => c.Id == id);
                _authSource.SetCategory(id);
                categoryName = category.Name;
            }
            
            CheckEmptyState();
            
            RunOnUiThread(delegate
            {
                SupportActionBar.Title = categoryName;
                _authListAdapter.NotifyDataSetChanged();
                _authList.ScheduleLayoutAnimation();
                
                _authList.AnimationEnd += delegate
                {
                    ScrollToPosition(0);
                };
            });
        }

        private void ScrollToPosition(int position)
        {
            if(position < 0 || position > _authSource.GetView().Count - 1)
                return;
            
            _authList.SmoothScrollToPosition(position);
            _appBarLayout.SetExpanded(true);
        }

        private void Tick()
        {
            RunOnUiThread(delegate {
                _authListAdapter.Tick();
            });
        }

        private void OnAuthenticatorClick(object sender, int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("code", auth.GetCode());
            clipboard.PrimaryClip = clip;

            ShowSnackbar(Resource.String.copiedToClipboard, Snackbar.LengthShort);
        }

        private void OnAuthenticatorOptionsClick(object sender, int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var bundle = new Bundle();
            bundle.PutInt("type", (int) auth.Type);
            bundle.PutLong("counter", auth.Counter);
            
            var fragment = new AuthenticatorMenuBottomSheet {Arguments = bundle};

            fragment.ClickRename += delegate { OpenRenameDialog(position); };
            fragment.ClickChangeIcon += delegate { OpenIconDialog(position); };
            fragment.ClickAssignCategories += delegate { OpenCategoriesDialog(position); };
            fragment.ClickShowQrCode += delegate { OpenQrCodeDialog(position); };
            fragment.ClickDelete += delegate { OpenDeleteDialog(position); };
            
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenQrCodeDialog(int position)
        {
            var auth = _authSource.Get(position);
            
            if(auth == null)
                return;

            string uri;

            try
            {
                uri = auth.GetOtpAuthUri();
            }
            catch(NotSupportedException)
            {
                ShowSnackbar(Resource.String.qrCodeNotSupported, Snackbar.LengthShort);
                return;
            }

            var bundle = new Bundle();
            bundle.PutString("uri", uri);
            
            var fragment = new QrCodeBottomSheet {Arguments = bundle};
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenDeleteDialog(int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                try
                {
                    await _authSource.Delete(position);
                }
                catch
                {
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }
                
                try
                {
                    await _customIconSource.CullUnused();
                }
                catch
                {
                    // ignored
                }

                RunOnUiThread(delegate { _authListAdapter.NotifyItemRemoved(position); });
                CheckEmptyState();
                
                _preferences.BackupRequired = BackupRequirement.WhenPossible;
                await NotifyWearAppOfChange();
            });
            
            builder.SetNegativeButton(Resource.String.cancel, delegate { });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            if(_authSource == null)
                return;
            
            var fragment = new AddMenuBottomSheet();
            fragment.ClickQrCode += delegate
            {
                var subFragment = new ScanQRCodeBottomSheet();
                subFragment.ClickFromCamera += async delegate { await RequestPermissionThenScanQRCode(); };
                subFragment.ClickFromGallery += delegate { StartFilePickActivity("image/*", RequestQrCode); };
                subFragment.Show(SupportFragmentManager, subFragment.Tag);
            };
            
            fragment.ClickEnterKey += OpenAddDialog;
            fragment.ClickRestore += delegate
            {
                StartFilePickActivity(Backup.MimeType, RequestRestore);
            };
            
            fragment.ClickImport += delegate
            {
                OpenImportMenu();
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }
        #endregion

        #region QR Code Scanning
        private async Task ScanQRCodeFromCamera()
        {
            var options = new MobileBarcodeScanningOptions
            {
                PossibleFormats = new List<BarcodeFormat>
                {
                    BarcodeFormat.QR_CODE
                },
                TryHarder = true,
                AutoRotate = true
            };

            var overlay = LayoutInflater.Inflate(Resource.Layout.scanOverlay, null);

            var scanner = new MobileBarcodeScanner
            {
                UseCustomOverlay = true, 
                CustomOverlay = overlay
            };
            
            var flashButton = overlay.FindViewById<MaterialButton>(Resource.Id.buttonFlash);
            flashButton.Click += delegate
            {
                scanner.ToggleTorch();
            };

            var hasFlashlight = PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash);
            flashButton.Visibility = hasFlashlight ? ViewStates.Visible : ViewStates.Gone;
            
            _preventBackupReminder = true;
            var result = await scanner.Scan(options);

            if(result == null)
                return;

            await ParseQRCodeScanResult(result.Text);
        }

        private async Task ScanQRCodeFromImage(Uri uri)
        {
            Bitmap bitmap;

            try
            {
                var data = await FileUtil.ReadFile(this, uri);
                bitmap = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            if(bitmap == null)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }

            var reader = new BarcodeReader<Bitmap>(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    PossibleFormats = new List<BarcodeFormat> {BarcodeFormat.QR_CODE}, TryHarder = true
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
                result = reader.Decode(source);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            
            if(result == null)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }
            
            await ParseQRCodeScanResult(result.Text);
        }

        private async Task ParseQRCodeScanResult(string uri)
        {
            if(uri.StartsWith("otpauth-migration"))
                await OnOtpAuthMigrationScan(uri);
            else if(uri.StartsWith("otpauth"))
                await OnOtpAuthScan(uri);
            else
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            _preferences.BackupRequired = BackupRequirement.Urgent;
            await NotifyWearAppOfChange();
        }

        private async Task OnOtpAuthScan(string uri)
        {
            Authenticator auth;

            try
            {
                auth = Authenticator.FromOtpAuthUri(uri, _iconResolver);
            }
            catch
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            if(_authSource.Exists(auth))
            {
                ShowSnackbar(Resource.String.duplicateAuthenticator, Snackbar.LengthShort);
                return;
            }

            int position;

            try
            {
                position = await _authSource.Add(auth);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if(_authSource.CategoryId != null)
            {
                await _authSource.AddToCategory(auth.Secret, _authSource.CategoryId);
                _authSource.UpdateView();
            }
            
            CheckEmptyState();
            
            RunOnUiThread(delegate
            {
                _authListAdapter.NotifyItemInserted(position);
                ScrollToPosition(position);
            });
            
            ShowSnackbar(Resource.String.scanSuccessful, Snackbar.LengthShort);
        }

        private async Task OnOtpAuthMigrationScan(string uri)
        {
            OtpAuthMigration migration;
            
            try
            {
                migration = OtpAuthMigration.FromOtpAuthMigrationUri(uri);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            var authenticators = new List<Authenticator>();
            
            foreach(var item in migration.Authenticators)
            {
                Authenticator auth;

                try
                {
                    auth = Authenticator.FromOtpAuthMigrationAuthenticator(item, _iconResolver);
                }
                catch(ArgumentException)
                {
                    continue;
                }
                
                authenticators.Add(auth);
            }

            try
            {
                await _authSource.AddMany(authenticators);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            
            SwitchCategory(null);
            RunOnUiThread(_authListAdapter.NotifyDataSetChanged);
            
            var message = String.Format(GetString(Resource.String.restoredFromMigration), authenticators.Count);
            ShowSnackbar(message, Snackbar.LengthLong);
        }

        private async Task RequestPermissionThenScanQRCode()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            else
                await ScanQRCodeFromCamera();
        }
        #endregion

        #region Restore / Import
        private void OpenImportMenu()
        {
            var fragment = new ImportBottomSheet();
            fragment.ClickGoogleAuthenticator += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Google-Authenticator");
            };
            
            // Use */* mime-type for most binary files because some files might not show on older Android versions
            // Use */* for json also, because application/json doesn't work
            
            fragment.ClickAuthenticatorPlus += delegate
            {
                StartFilePickActivity("*/*", RequestImportAuthenticatorPlus);
            };
            
            fragment.ClickAndOtp += delegate
            {
                StartFilePickActivity("*/*", RequestImportAndOtp);
            };
            
            fragment.ClickFreeOtpPlus += delegate
            {
                StartFilePickActivity("*/*", RequestImportFreeOtpPlus);
            };
            
            fragment.ClickAegis += delegate
            {
                StartFilePickActivity("*/*", RequestImportAegis);
            };
            
            fragment.ClickBitwarden += delegate
            {
                StartFilePickActivity("*/*", RequestImportBitwarden);
            };
            
            fragment.ClickWinAuth += delegate
            {
                StartFilePickActivity("text/plain", RequestImportWinAuth);
            };
            
            fragment.ClickAuthy += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Authy");
            };
            
            fragment.ClickTotpAuthenticator += delegate
            {
                StartFilePickActivity("*/*", RequestImportTotpAuthenticator);
            };
            
            fragment.ClickBlizzardAuthenticator += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Blizzard-Authenticator");
            };
            
            fragment.ClickSteam += delegate
            {
                StartWebBrowserActivity(Constants.GitHubRepo + "/wiki/Importing-from-Steam");
            };
            
            fragment.ClickUriList += delegate
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
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            
            if(data.Length == 0)
            {
                ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                return;
            }

            async Task<RestoreResult> DecryptAndRestore(string password)
            {
                Backup backup = null;

                await Task.Run(delegate
                {
                    backup = Backup.FromBytes(data, password);
                });
                
                return await RestoreBackup(backup, true);
            }

            if(Backup.IsReadableWithoutPassword(data))
            {
                RestoreResult result;
                
                try
                {
                    result = await DecryptAndRestore(null);
                }
                catch
                {
                    ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                    return;
                }
                
                await FinaliseRestore(result);
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
            var sheet = new BackupPasswordBottomSheet {Arguments = bundle};
            
            sheet.PasswordEntered += async (_, password) =>
            {
                sheet.SetBusyText(Resource.String.decrypting);
                
                try
                {
                    var result = await DecryptAndRestore(password);
                    sheet.Dismiss();
                    await FinaliseRestore(result);
                }
                catch
                {
                    sheet.Error = GetString(Resource.String.restoreError);
                    sheet.SetBusyText(null);
                }
            };
            
            sheet.Show(SupportFragmentManager, sheet.Tag);
        }

        private async Task<RestoreResult> RestoreBackup(Backup backup, bool shouldUpdateExisting)
        {
            if(backup.Authenticators == null)
                throw new ArgumentException();
            
            int authsAdded;
            var authsUpdated = 0;

            if(shouldUpdateExisting)
                (authsAdded, authsUpdated) = await _authSource.AddOrUpdateMany(backup.Authenticators);
            else
                authsAdded = await _authSource.AddMany(backup.Authenticators);

            var categoryCount = backup.Categories != null
                ? await _categorySource.AddMany(backup.Categories)
                : 0;

            if(backup.AuthenticatorCategories != null)
            {
                if(shouldUpdateExisting)
                    await _authSource.AddOrUpdateManyCategoryBindings(backup.AuthenticatorCategories);
                else
                    await _authSource.AddManyCategoryBindings(backup.AuthenticatorCategories);
            }
           
            var customIconCount = backup.CustomIcons != null
                ? await _customIconSource.AddMany(backup.CustomIcons)
                : 0;

            try
            {
                await _customIconSource.CullUnused();
            }
            catch
            {
                // ignored
            }

            return new RestoreResult(authsAdded, authsUpdated, categoryCount, customIconCount);
        }

        private async Task ImportFromUri(BackupConverter converter, Uri uri)
        {
            byte[] data;

            try
            {
                data = await FileUtil.ReadFile(this, uri);
            }
            catch
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            
            async Task ConvertAndRestore(string password)
            {
                var backup = await converter.Convert(data, password);
                var result = await RestoreBackup(backup, false);
                await FinaliseRestore(result);
                _preferences.BackupRequired = BackupRequirement.Urgent;
            }

            void ShowPasswordSheet()
            {
                var bundle = new Bundle();
                bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Enter);
                var sheet = new BackupPasswordBottomSheet {Arguments = bundle};
                
                sheet.PasswordEntered += async (_, password) =>
                {
                    sheet.SetBusyText(Resource.String.decrypting);
                    
                    try
                    {
                        await ConvertAndRestore(password);
                        sheet.Dismiss();
                    }
                    catch
                    {
                        sheet.Error = GetString(Resource.String.restoreError);
                        sheet.SetBusyText(null);
                    }
                };
                sheet.Show(SupportFragmentManager, sheet.Tag);
            }

            switch(converter.PasswordPolicy)
            {
                case BackupConverter.BackupPasswordPolicy.Never:
                    try
                    {
                        await ConvertAndRestore(null);
                    }
                    catch
                    {
                        ShowSnackbar(Resource.String.importError, Snackbar.LengthShort);
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

        private async Task FinaliseRestore(IResult result)
        {
            ShowSnackbar(result.ToString(this), Snackbar.LengthShort);
            
            if(result.IsVoid())
                return;
            
            CheckEmptyState();
            
            RunOnUiThread(delegate
            {
                _authListAdapter.NotifyDataSetChanged();
                _authList.ScheduleLayoutAnimation();
            });
          
            Tick();
            await NotifyWearAppOfChange();
        }
        #endregion

        #region Backup
        private void OpenBackupMenu()
        {
            var fragment = new BackupBottomSheet();
            
            void ShowPicker(string mimeType, int requestCode, string fileExtension)
            {
                StartFileSaveActivity(mimeType, requestCode, $"backup-{DateTime.Now:yyyy-MM-dd_HHmmss}.{fileExtension}");
            }
            
            fragment.ClickBackupFile += delegate
            {
                ShowPicker(Backup.MimeType, RequestBackupFile, Backup.FileExtension);
            };
            
            fragment.ClickHtmlFile += delegate
            {
                ShowPicker(HtmlBackup.MimeType, RequestBackupHtml, HtmlBackup.FileExtension);
            };

            fragment.ClickUriList += delegate
            {
                ShowPicker(UriListBackup.MimeType, RequestBackupUriList, UriListBackup.FileExtension);
            };
            
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task BackupToFile(Uri destination)
        {
            async Task DoBackup(string password)
            {
                var backup = new Backup(
                    _authSource.GetAll(),
                    _categorySource.GetAll(),
                    _authSource.CategoryBindings,
                    _customIconSource.GetAll()
                );
                
                try
                {
                    byte[] data = null;
                    await Task.Run(delegate { data = backup.ToBytes(password); });
                    await FileUtil.WriteFile(this, destination, data);
                }
                catch
                {
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }
                
                FinaliseBackup();
            }

            if(_preferences.PasswordProtected && _preferences.DatabasePasswordBackup)
            {
                var password = await SecureStorageWrapper.GetDatabasePassword();
                await DoBackup(password);
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Set); 
            var fragment = new BackupPasswordBottomSheet {Arguments = bundle};
            
            fragment.PasswordEntered += async (sender, password) =>
            {
                var busyText = !String.IsNullOrEmpty(password) ? Resource.String.encrypting : Resource.String.saving; 
                fragment.SetBusyText(busyText);
                await DoBackup(password);
                ((BackupPasswordBottomSheet) sender).Dismiss();
            };

            fragment.Cancel += (sender, _) =>
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
                var backup = await HtmlBackup.FromAuthenticators(this, _authSource.GetAll());
                await FileUtil.WriteFile(this, destination, backup.ToString());
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            FinaliseBackup();
        }
        
        private async Task BackupToUriListFile(Uri destination)
        {
            try
            {
                var backup = UriListBackup.FromAuthenticators(_authSource.GetAll());
                await FileUtil.WriteFile(this, destination, backup.ToString());
            }
            catch
            {
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
            if(!_authSource.GetAll().Any())
                return;

            if(_preferences.BackupRequired != BackupRequirement.Urgent || _preferences.AutoBackupEnabled)
                return;

            _lastBackupReminderTime = DateTime.UtcNow;
            var snackbar = Snackbar.Make(_coordinatorLayout, Resource.String.backupReminder, Snackbar.LengthLong);
            snackbar.SetAnchorView(_addButton);
            snackbar.SetAction(Resource.String.backupNow, delegate
            {
                OpenBackupMenu();
            });
            
            var callback = new SnackbarCallback();
            callback.Dismiss += (_, e) =>
            {
                if(e == Snackbar.Callback.DismissEventSwipe)
                    _preferences.BackupRequired = BackupRequirement.NotRequired;
            };

            snackbar.AddCallback(callback);
            snackbar.Show();
        }
        #endregion

        #region Add Dialog
        private void OpenAddDialog(object sender, EventArgs e)
        {
            var fragment = new AddAuthenticatorBottomSheet();
            fragment.Add += OnAddDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAddDialogSubmit(object sender, Authenticator auth)
        {
            var dialog = (AddAuthenticatorBottomSheet) sender;

            if(_authSource.Exists(auth))
            {
                dialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }

            int position;

            try
            {
                if(_authSource.CategoryId == null)
                    position = await _authSource.Add(auth);
                else
                {
                    await _authSource.Add(auth);
                    await _authSource.AddToCategory(auth.Secret, _authSource.CategoryId);
                    position = _authSource.GetPosition(auth.Secret);
                }
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            
            CheckEmptyState();

            RunOnUiThread(delegate
            {
                _authListAdapter.NotifyItemInserted(position);
                ScrollToPosition(position);
            });
            
            dialog.Dismiss();
            _preferences.BackupRequired = BackupRequirement.Urgent;
            
            await NotifyWearAppOfChange();
        }
        #endregion

        #region Rename Dialog
        private void OpenRenameDialog(int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutString("issuer", auth.Issuer);
            bundle.PutString("username", auth.Username);
            
            var fragment = new RenameAuthenticatorBottomSheet {Arguments = bundle};
            fragment.Rename += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, RenameAuthenticatorBottomSheet.RenameEventArgs e)
        {
            var auth = _authSource.Get(e.ItemPosition);

            if(auth == null)
                return;
            
            auth.Issuer = e.Issuer;
            auth.Username = e.Username;
            auth.Icon ??= _iconResolver.FindServiceKeyByName(auth.Issuer);
            
            try
            {
                await _authSource.UpdateSingle(auth);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(e.ItemPosition); });
            _preferences.BackupRequired = BackupRequirement.WhenPossible;
            await NotifyWearAppOfChange();
        }
        #endregion

        #region Icon Dialog
        private void OpenIconDialog(int position)
        {
            var bundle = new Bundle();
            bundle.PutInt("position", position);

            var fragment = new ChangeIconBottomSheet {Arguments = bundle};
            fragment.IconSelect += OnIconDialogIconSelected;
            fragment.UseCustomIconClick += delegate 
            {
                _customIconApplyPosition = position;
                StartFilePickActivity("image/*", RequestCustomIcon);
            };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnIconDialogIconSelected(object sender, ChangeIconBottomSheet.IconSelectedEventArgs e)
        {
            var auth = _authSource.Get(e.ItemPosition);

            if(auth == null)
                return;

            var oldIcon = auth.Icon;
            auth.Icon = e.Icon;

            try
            {
                await _authSource.UpdateSingle(auth);
            }
            catch
            {
                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            
            try
            {
                await _customIconSource.CullUnused();
            }
            catch
            {
                // ignored
            }

            _preferences.BackupRequired = BackupRequirement.WhenPossible;
            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(e.ItemPosition); });
            await NotifyWearAppOfChange();

            ((ChangeIconBottomSheet) sender).Dismiss();
        }
        #endregion

        #region Custom Icons
        private async Task SetCustomIcon(Uri source, int position)
        {
            var decoder = new CustomIconDecoder();
            CustomIcon icon;

            try
            {
                var data = await FileUtil.ReadFile(this, source);
                icon = await decoder.Decode(data);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            
            var auth = _authSource.Get(position);

            if(auth == null || auth.Icon == CustomIcon.Prefix + icon.Id)
                return;

            try
            {
                await _customIconSource.Add(icon);
            }
            catch(ArgumentException)
            {
                // Duplicate icon, ignore 
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            var oldIcon = auth.Icon;
            auth.Icon = CustomIcon.Prefix + icon.Id;

            try
            {
                await _authSource.UpdateSingle(auth);
            }
            catch
            {
                try
                {
                    await _customIconSource.Delete(icon.Id);
                }
                catch
                {
                    // ignored, not much can be done at this point
                }

                auth.Icon = oldIcon;
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            try
            {
                await _customIconSource.CullUnused();
            }
            catch
            {
                // this shouldn't fail, but ignore if it does
            }
            
            _preferences.BackupRequired = BackupRequirement.WhenPossible;
            
            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(position); });
            await NotifyWearAppOfChange();
        }
        #endregion

        #region Categories
        private void OpenCategoriesDialog(int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;
            
            var categoryIds = _categorySource.GetView().Select(c => c.Id).ToArray();
            var categoryNames = _categorySource.GetView().Select(c => c.Name).ToArray();

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutStringArray("categoryIds", categoryIds);
            bundle.PutStringArray("categoryNames", categoryNames);
            bundle.PutStringArray("assignedCategoryIds", _authSource.GetCategories(position));

            var fragment = new AssignCategoriesBottomSheet {Arguments = bundle};
            fragment.CategoryClick += OnCategoriesDialogCategoryClick;
            fragment.ManageCategoriesClick += delegate
            {
                _updateOnActivityResume = true;
                StartActivity(typeof(ManageCategoriesActivity));
                fragment.Dismiss();
            };
            fragment.Close += OnCategoriesDialogClose;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnCategoriesDialogClose(object sender, EventArgs e)
        {
            if(_authSource.CategoryId != null)
            {
                _authSource.UpdateView();
                _authListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            }
        }

        private async void OnCategoriesDialogCategoryClick(object sender, AssignCategoriesBottomSheet.CategoryClickedEventArgs e)
        {
            var categoryId = _categorySource.Get(e.CategoryPosition).Id;
            var authSecret = _authSource.Get(e.ItemPosition).Secret;

            try
            {
                if(e.IsChecked)
                    await _authSource.AddToCategory(authSecret, categoryId);
                else
                    await _authSource.RemoveFromCategory(authSecret, categoryId);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
            }
        }
        #endregion

        #region Wear OS
        public async void OnCapabilityChanged(ICapabilityInfo capabilityInfo)
        {
            await DetectWearOSCapability();
        }

        private void DetectGoogleAPIsAvailability()
        {
            _areGoogleAPIsAvailable = GoogleApiAvailabilityLight.Instance.IsGooglePlayServicesAvailable(this) == 
                                      ConnectionResult.Success;
        }

        private async Task DetectWearOSCapability()
        {
            if(!_areGoogleAPIsAvailable)
            {
                _hasWearAPIs = false;
                _hasWearCompanion = false;
                return;
            }

            try
            {
                var capabiltyInfo = await WearableClass.GetCapabilityClient(this)
                    .GetCapabilityAsync(WearRefreshCapability, CapabilityClient.FilterReachable);

                _hasWearAPIs = true;
                _hasWearCompanion = capabiltyInfo.Nodes.Count > 0;
            }
            catch(ApiException)
            {
                _hasWearAPIs = false;
                _hasWearCompanion = false;
            }
        }

        private async Task NotifyWearAppOfChange()
        {
            if(!_hasWearCompanion)
                return;

            var nodes = (await WearableClass.GetCapabilityClient(this)
                .GetCapabilityAsync(WearRefreshCapability, CapabilityClient.FilterReachable)).Nodes;

            var client = WearableClass.GetMessageClient(this);

            foreach(var node in nodes)
                await client.SendMessageAsync(node.Id, WearRefreshCapability, new byte[] { });
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
        
        private void StartFilePickActivity(string mimeType, int requestCode)
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);

            BaseApplication.PreventNextLock = true;

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch(ActivityNotFoundException)
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

            BaseApplication.PreventNextLock = true;
            
            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch(ActivityNotFoundException)
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
            catch(ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.webBrowserMissing, Snackbar.LengthLong); 
            }
        }
        #endregion
    }
}