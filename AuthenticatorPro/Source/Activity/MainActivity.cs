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
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Callback;
using AuthenticatorPro.Data;
using AuthenticatorPro.Data.Backup;
using AuthenticatorPro.Data.Backup.Converter;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.Fragment;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Java.Nio;
using SQLite;
using ZXing;
using ZXing.Common;
using ZXing.Mobile;
using Result = Android.App.Result;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using Timer = System.Timers.Timer;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/MainActivityTheme", MainLauncher = true,
              Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustPan,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    internal class MainActivity : DayNightActivity, CapabilityClient.IOnCapabilityChangedListener
    {
        private const string WearRefreshCapability = "refresh";
        private const int PermissionCameraCode = 0;

        // Request codes
        private const int ResultLogin = 0;
        private const int ResultRestore = 1;
        private const int ResultBackupFile = 2;
        private const int ResultBackupHtml = 3;
        private const int ResultQRCode = 4;
        private const int ResultCustomIcon = 5;
        private const int ResultSettingsRecreate = 6;
        private const int ResultImportAuthenticatorPlus = 7;
        private const int ResultImportWinAuth = 8;

        private const int BackupReminderThresholdMinutes = 120;
        private const int AppLockThresholdMinutes = 1;

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
        private MaterialButton _viewGuideButton;

        private AuthenticatorListAdapter _authListAdapter;
        private AuthenticatorSource _authSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;

        // State
        private SQLiteAsyncConnection _connection;
        private Timer _timer;
        private DateTime _pauseTime;
        private DateTime _lastBackupReminderTime;
        private bool _isAuthenticated;
        private bool _returnedFromResult;
        private bool _updateOnActivityResume;
        private int _customIconApplyPosition;

        // Wear OS State
        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        // Activity lifecycle synchronisation
        // Async activity lifecycle methods pass control to the next method, Resume is called before Create has finished.
        // Hand control to the next method when it's safe to do so.
        private readonly SemaphoreSlim _onCreateSemaphore;
        private readonly SemaphoreSlim _onResumeSemaphore;


        public MainActivity()
        {
            _onCreateSemaphore = new SemaphoreSlim(1, 1);
            _onResumeSemaphore = new SemaphoreSlim(1, 1);
        }

        #region Activity Lifecycle
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            await _onCreateSemaphore.WaitAsync();
            MobileBarcodeScanner.Initialize(Application);
            
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activityMain);
            InitViews();

            if(savedInstanceState != null)
            {
                _isAuthenticated = savedInstanceState.GetBoolean("isAuthenticated");
                _pauseTime = new DateTime(savedInstanceState.GetLong("pauseTime"));
                _lastBackupReminderTime = new DateTime(savedInstanceState.GetLong("lastBackupReminderTime"));
            }
            else
            {
                _isAuthenticated = false;
                _pauseTime = DateTime.MinValue;
                _lastBackupReminderTime = DateTime.MinValue;
            }

            try
            {
                _connection = await Database.Connect(this);
            }
            catch(Exception)
            {
                ShowDatabaseErrorDialog();
                return;
            }
            
            _categorySource = new CategorySource(_connection);
            _customIconSource = new CustomIconSource(_connection);
            _authSource = new AuthenticatorSource(_connection);
            RunOnUiThread(InitAuthenticatorList);

            _timer = new Timer {
                Interval = 1000,
                AutoReset = true
            };
            
            _timer.Elapsed += (_, _) =>
            {
                Tick();
            };

            _updateOnActivityResume = true;
            _onCreateSemaphore.Release();
            
            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var firstLaunch = prefs.GetBoolean("firstLaunch", true);
            
            if(firstLaunch)
                StartActivity(typeof(IntroActivity));

            DetectGoogleAPIsAvailability();
            await DetectWearOSCapability();
        }

        protected override async void OnResume()
        {
            base.OnResume();
            
            await _onCreateSemaphore.WaitAsync();
            _onCreateSemaphore.Release();

            await _onResumeSemaphore.WaitAsync();

            if(RequiresAuthentication())
            {
                if((DateTime.UtcNow - _pauseTime).TotalMinutes >= AppLockThresholdMinutes)
                    _isAuthenticated = false;
            
                if(!_isAuthenticated)
                {
                    _updateOnActivityResume = true;
                    _onResumeSemaphore.Release();
                    StartActivityForResult(typeof(LoginActivity), ResultLogin);
                    return;
                }
            }

            if(_updateOnActivityResume)
            {
                _updateOnActivityResume = false;

                RunOnUiThread(delegate { _authList.Visibility = ViewStates.Invisible; });
                await _customIconSource.Update();
                await UpdateList();
                await _categorySource.Update();

                // Currently visible category has been deleted
                if(_authSource.CategoryId != null &&
                   _categorySource.GetView().FirstOrDefault(c => c.Id == _authSource.CategoryId) == null)
                    await SwitchCategory(null);
            }
            
            _onResumeSemaphore.Release();
            RunOnUiThread(CheckEmptyState);
            
            if(_authSource.GetView().Any())
                Tick(true);

            var showBackupReminders = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_showBackupReminders", true);
           
            if(!_returnedFromResult && showBackupReminders && (DateTime.UtcNow - _lastBackupReminderTime).TotalMinutes > BackupReminderThresholdMinutes)
                RemindBackup();

            _returnedFromResult = false;

            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).AddListenerAsync(this, WearRefreshCapability);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean("isAuthenticated", _isAuthenticated);
            outState.PutLong("pauseTime", _pauseTime.Ticks);
            outState.PutLong("lastBackupReminderTime", _lastBackupReminderTime.Ticks);
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();

            if(_connection != null)
                await _connection.CloseAsync();
        }
        
        protected override async void OnPause()
        {
            base.OnPause();

            _timer?.Stop();
            _pauseTime = DateTime.UtcNow;

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
            _returnedFromResult = true;
            
            if(resultCode != Result.Ok)
                return;

            switch(requestCode)
            {
                case ResultLogin:
                    _isAuthenticated = true;
                    return;
                
                case ResultSettingsRecreate:
                    Recreate();
                    return;
            }

            await _onResumeSemaphore.WaitAsync();
            _onResumeSemaphore.Release();
            
            switch(requestCode)
            {
                case ResultRestore:
                    await BeginRestore(intent.Data);
                    break;
                
                case ResultBackupFile:
                    BeginBackupToFile(intent.Data);
                    break;
                
                case ResultBackupHtml:
                    await DoHtmlBackup(intent.Data);
                    break;
                
                case ResultCustomIcon:
                    await SetCustomIcon(intent.Data);
                    break;
                
                case ResultQRCode:
                    await ScanQRCodeFromImage(intent.Data);
                    break;
                
                case ResultImportAuthenticatorPlus:
                    await DoImport(new AuthenticatorPlusBackupConverter(), intent.Data);
                    break;
                
                case ResultImportWinAuth:
                    await DoImport(new WinAuthBackupConverter(), intent.Data);
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
            
            var fragment = new MainMenuBottomSheet(_categorySource, _authSource.CategoryId);
            fragment.CategoryClick += async (_, id) =>
            {
                await SwitchCategory(id);
                RunOnUiThread(fragment.Dismiss);
            };

            fragment.BackupClick += delegate
            {
                if(!_authSource.GetAll().Any())
                {
                    ShowSnackbar(Resource.String.noAuthenticators, Snackbar.LengthShort);
                    return;
                }

                OpenBackupMenu();
            };

            fragment.ManageCategoriesClick += delegate
            {
                _updateOnActivityResume = true;
                StartActivity(typeof(ManageCategoriesActivity));
            };
            
            fragment.SettingsClick += delegate
            {
                StartActivityForResult(typeof(SettingsActivity), ResultSettingsRecreate);
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
            
            if(_authSource?.CategoryId != null)
            {
                await SwitchCategory(null);
                return;
            }
            
            base.OnBackPressed();
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

        #region Authenticator List
        private void InitViews()
        {
            _toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);

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

            _coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.coordinatorLayout);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddButtonClick;

            _authList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);
            _emptyMessageText = FindViewById<TextView>(Resource.Id.textEmptyMessage);
            _viewGuideButton = FindViewById<MaterialButton>(Resource.Id.buttonViewGuide);
            _viewGuideButton.Click += delegate { StartActivity(typeof(GuideActivity)); };
        }

        private void InitAuthenticatorList()
        {
            var viewModePref = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetString("pref_viewMode", "default");

            var viewMode = viewModePref switch
            {
                "compact" => AuthenticatorListAdapter.ViewMode.Compact,
                "tile" => AuthenticatorListAdapter.ViewMode.Tile,
                _ => AuthenticatorListAdapter.ViewMode.Default
            };

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

            var minColumnWidth = viewMode switch
            {
                AuthenticatorListAdapter.ViewMode.Compact => 300,
                AuthenticatorListAdapter.ViewMode.Tile => 170,
                _ => 340
            };

            var layout = new AutoGridLayoutManager(this, minColumnWidth);
            _authList.SetLayoutManager(layout);

            _authList.AddItemDecoration(new GridSpacingItemDecoration(this, layout, 8));
            _authList.HasFixedSize = true;

            var animation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            var callback = new ReorderableListTouchHelperCallback(_authListAdapter, layout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);
        }

        private async Task UpdateList(bool viewOnly = false)
        {
            if(!viewOnly)
            {
                RunOnUiThread(delegate { _progressBar.Visibility = ViewStates.Visible; });
                await _authSource.Update();
            }

            RunOnUiThread(delegate
            {
                _authListAdapter.NotifyDataSetChanged();
                _authList.ScheduleLayoutAnimation();

                if(!viewOnly)
                    _progressBar.Visibility = ViewStates.Invisible;
            });
        }

        private void CheckEmptyState()
        {
            if(!_authSource.GetView().Any())
            {
                _authList.Visibility = ViewStates.Invisible;
                AnimUtil.FadeInView(_emptyStateLayout, 500, true);

                if(_authSource.CategoryId == null)
                {
                    _emptyMessageText.SetText(Resource.String.noAuthenticatorsHelp);
                    _viewGuideButton.Visibility = ViewStates.Visible;
                }
                else
                {
                    _emptyMessageText.SetText(Resource.String.noAuthenticatorsMessage);
                    _viewGuideButton.Visibility = ViewStates.Gone;
                }
                
                _timer.Stop();
            }
            else
            {
                _emptyStateLayout.Visibility = ViewStates.Invisible;
                AnimUtil.FadeInView(_authList, 100, true);
                _timer.Start();
            }
        }

        private async Task SwitchCategory(string id)
        {
            if(id == _authSource.CategoryId)
            {
                RunOnUiThread(CheckEmptyState);
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
            
            RunOnUiThread(delegate { SupportActionBar.Title = categoryName; });

            await UpdateList(true);

            RunOnUiThread(delegate
            {
                _authList.Visibility = ViewStates.Invisible;
                CheckEmptyState();
                ScrollToPosition(0);
            });
        }

        private void Tick(bool invalidateCache = false)
        {
            RunOnUiThread(delegate {
                _authListAdapter.Tick(invalidateCache);
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

            var fragment = new EditMenuBottomSheet(auth.Type, auth.Counter);
            fragment.ClickRename += delegate { OpenRenameDialog(position); };
            fragment.ClickChangeIcon += delegate { OpenIconDialog(position); };
            fragment.ClickAssignCategories += delegate { OpenCategoriesDialog(position); };
            fragment.ClickDelete += delegate { OpenDeleteDialog(position); };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenDeleteDialog(int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                var icon = _authSource.Get(position).Icon;

                try
                {
                    await _authSource.Delete(position);
                }
                catch
                {
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }
                
                await TryCleanupCustomIcon(icon);

                RunOnUiThread(delegate
                {
                    _authListAdapter.NotifyItemRemoved(position);
                    CheckEmptyState();
                });
                
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
                subFragment.ClickFromGallery += delegate { OpenFilePicker("image/*", ResultQRCode); };
                subFragment.Show(SupportFragmentManager, subFragment.Tag);
            };
            
            fragment.ClickEnterKey += OpenAddDialog;
            fragment.ClickRestore += delegate
            {
                OpenFilePicker("application/octet-stream", ResultRestore);
            };
            
            fragment.ClickImport += delegate
            {
                var sub = new ImportBottomSheet();
                sub.ClickAuthenticatorPlus += (_, _) =>
                {
                    OpenFilePicker("application/octet-stream", ResultImportAuthenticatorPlus);
                };
                sub.ClickWinAuth += (_, _) =>
                {
                    OpenFilePicker("text/plain", ResultImportWinAuth);
                };
                sub.Show(SupportFragmentManager, fragment.Tag);
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
                TryHarder = true
            };

            var scanner = new MobileBarcodeScanner();
            var result = await scanner.Scan(options);

            if(result == null)
                return;

            await ParseQRCodeScanResult(result);
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

            var reader = new BarcodeReader(null, null, ls => new GlobalHistogramBinarizer(ls))
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

                var source = new RGBLuminanceSource(bytes, bitmap.Width, bitmap.Height,
                    RGBLuminanceSource.BitmapFormat.RGBA32);
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
            
            await ParseQRCodeScanResult(result);
        }

        private async Task ParseQRCodeScanResult(ZXing.Result result)
        {
            if(result.Text.StartsWith("otpauth-migration"))
                await OnOtpAuthMigrationScan(result.Text);
            else if(result.Text.StartsWith("otpauth"))
                await OnOtpAuthScan(result.Text);
            else
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            await NotifyWearAppOfChange();

            PreferenceManager.GetDefaultSharedPreferences(this)
                .Edit()
                .PutBoolean("needsBackup", true)
                .Commit();
        }

        private async Task OnOtpAuthScan(string uri)
        {
            Authenticator auth;

            try
            {
                auth = Authenticator.FromOtpAuthUri(uri);
            }
            catch
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            if(_authSource.IsDuplicate(auth))
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
            
            RunOnUiThread(delegate
            {
                CheckEmptyState();
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
                    auth = Authenticator.FromOtpAuthMigrationAuthenticator(item);
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

            await SwitchCategory(null);
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
        private async Task BeginRestore(Uri uri)
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

            async Task<Tuple<int, int>> DecryptAndRestore(string password)
            {
                var backup = Backup.FromBytes(data, password);
                return await DoRestore(backup);
            }

            // Open and closed curly brace (file is not encrypted)
            if(data[0] == '{' && data[^1] == '}')
            {
                int authCount, categoryCount;
                
                try
                {
                    (authCount, categoryCount) = await DecryptAndRestore(null);
                }
                catch(ArgumentException)
                {
                    ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                    return;
                }
                
                await FinaliseRestore(authCount, categoryCount);
                return;
            }
            
            var sheet = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Enter);
            sheet.PasswordEntered += async (_, password) =>
            {
                try
                {
                    var (authCount, categoryCount) = await DecryptAndRestore(password);
                    sheet.Dismiss();
                    await FinaliseRestore(authCount, categoryCount);
                }
                catch(ArgumentException)
                {
                    sheet.Error = GetString(Resource.String.restoreError);
                }
            };
            sheet.Show(SupportFragmentManager, sheet.Tag);
        }
        
        private async Task<Tuple<int, int>> DoRestore(Backup backup)
        {
            if(backup.Authenticators == null)
                throw new ArgumentException();

            var authenticatorsAdded = backup.Authenticators != null
                ? await _authSource.AddMany(backup.Authenticators)
                : 0;

            var categoriesAdded = backup.Categories != null
                ? await _categorySource.AddMany(backup.Categories)
                : 0;
            
            if(backup.AuthenticatorCategories != null)
                _ = await _authSource.AddManyCategoryBindings(backup.AuthenticatorCategories);
            
            if(backup.CustomIcons != null)
                _ = await _customIconSource.AddMany(backup.CustomIcons);

            return new Tuple<int, int>(authenticatorsAdded, categoriesAdded);
        }

        private async Task DoImport(BackupConverter converter, Uri uri)
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
            
            BackupPasswordBottomSheet sheet;
            
            async Task<Tuple<int, int>> ConvertAndRestore(string password)
            {
                var backup = await converter.Convert(data, password);
                return await DoRestore(backup);
            }

            void ShowPasswordSheet()
            {
                sheet = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Enter);
                sheet.PasswordEntered += async (_, password) =>
                {
                    try
                    {
                        var (authCount, categoryCount) = await ConvertAndRestore(password);
                        sheet.Dismiss();
                        await FinaliseRestore(authCount, categoryCount);
                    }
                    catch(ArgumentException)
                    {
                        sheet.Error = GetString(Resource.String.restoreError);
                    }
                };
                sheet.Show(SupportFragmentManager, sheet.Tag);
            }

            switch(converter.PasswordPolicy)
            {
                case BackupConverter.BackupPasswordPolicy.Never:
                    try
                    {
                        var (authCount, categoryCount) = await ConvertAndRestore(null);
                        await FinaliseRestore(authCount, categoryCount);
                    }
                    catch(ArgumentException)
                    {
                        ShowSnackbar(Resource.String.restoreError, Snackbar.LengthShort);
                    }
                    break;
                
                case BackupConverter.BackupPasswordPolicy.Always:
                    ShowPasswordSheet();
                    break;
                
                case BackupConverter.BackupPasswordPolicy.Maybe:
                    try
                    {
                        var (authCount, categoryCount) = await ConvertAndRestore(null);
                        await FinaliseRestore(authCount, categoryCount);
                    }
                    catch(ArgumentException)
                    {
                        ShowPasswordSheet(); 
                    }
                    break;
            }
        }

        private async Task FinaliseRestore(int authCount, int categoryCount)
        {
            RunOnUiThread(CheckEmptyState);
            await UpdateList(true);

            var message = String.Format(GetString(Resource.String.restoredFromBackup), authCount, categoryCount);
            ShowSnackbar(message, Snackbar.LengthLong);

            await NotifyWearAppOfChange();
        }
        #endregion

        #region Backup
        private void OpenBackupMenu()
        {
            var fragment = new BackupBottomSheet();
            fragment.ClickBackupFile += delegate { StartBackupFileSaveActivity(); };
            fragment.ClickHtmlFile += delegate { StartBackupHtmlSaveActivity(); };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }
        
        private void StartBackupFileSaveActivity()
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, $"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

            try
            {
                StartActivityForResult(intent, ResultBackupFile);
            }
            catch(ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong); 
            }
        }
        
        private void StartBackupHtmlSaveActivity()
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("text/html");
            intent.PutExtra(Intent.ExtraTitle, $"backup-{DateTime.Now:yyyy-MM-dd}.html");

            try
            {
                StartActivityForResult(intent, ResultBackupHtml);
            }
            catch(ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong); 
            }
        }

        private void BeginBackupToFile(Uri uri)
        {
            var fragment = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Set);
            fragment.PasswordEntered += async (sender, password) =>
            {
                try
                {
                    await DoFileBackup(uri, password);
                }
                catch(Exception)
                {
                    ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                }

                PreferenceManager.GetDefaultSharedPreferences(this)
                    .Edit()
                    .PutBoolean("needsBackup", false)
                    .Commit();
                
                ((BackupPasswordBottomSheet) sender).Dismiss();
                ShowSnackbar(Resource.String.saveSuccess, Snackbar.LengthLong);
            };

            fragment.Cancel += (sender, _) =>
            {
                // TODO: Delete empty file only if we just created it
                // DocumentsContract.DeleteDocument(ContentResolver, uri);
                ((BackupPasswordBottomSheet) sender).Dismiss();
            };
            
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task DoHtmlBackup(Uri uri)
        {
            var backup = await HtmlBackup.FromAuthenticatorList(this, _authSource.GetAll());

            try
            {
                await FileUtil.WriteFile(this, uri, backup.ToString());
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            
            PreferenceManager.GetDefaultSharedPreferences(this)
                .Edit()
                .PutBoolean("needsBackup", false)
                .Commit();
            
            ShowSnackbar(Resource.String.saveSuccess, Snackbar.LengthLong);
        }

        private async Task DoFileBackup(Uri uri, string password)
        {
            var backup = new Backup(
                _authSource.GetAll(),
                _categorySource.GetAll(),
                _authSource.CategoryBindings,
                _customIconSource.GetAll()
            );

            var data = backup.ToBytes(password);
            await FileUtil.WriteFile(this, uri, data);
        }
        
        private void RemindBackup()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var needsBackup = prefs.GetBoolean("needsBackup", false) && _authSource.GetAll().Any();
            var autoBackupEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);

            if(!needsBackup || autoBackupEnabled)
                return;

            _lastBackupReminderTime = DateTime.UtcNow;
            var snackbar = Snackbar.Make(_coordinatorLayout, Resource.String.backupReminder, Snackbar.LengthLong);
            snackbar.SetAnchorView(_addButton);
            snackbar.SetAction(Resource.String.backupNow, _ =>
            {
                OpenBackupMenu();
            });
            
            var callback = new SnackbarCallback();
            callback.Dismiss += (_, e) =>
            {
                if(e == Snackbar.Callback.DismissEventSwipe)
                    prefs.Edit().PutBoolean("needsBackup", false).Commit();
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

            if(_authSource.IsDuplicate(auth))
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
                    await _authSource.AddToCategory(auth.Secret, _authSource.CategoryId);
                    position = _authSource.GetPosition(auth.Secret);
                }
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            RunOnUiThread(delegate
            {
                CheckEmptyState();
                _authListAdapter.NotifyItemInserted(position);
                ScrollToPosition(position);
            });
            
            await NotifyWearAppOfChange();

            PreferenceManager.GetDefaultSharedPreferences(this)
                .Edit()
                .PutBoolean("needsBackup", true)
                .Commit();
            
            dialog.Dismiss();
        }
        #endregion

        #region Rename Dialog
        private void OpenRenameDialog(int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var fragment = new RenameAuthenticatorBottomSheet(position, auth.Issuer, auth.Username);
            fragment.Rename += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, RenameAuthenticatorBottomSheet.RenameEventArgs e)
        {
            await _authSource.Rename(e.ItemPosition, e.Issuer, e.Username);
            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(e.ItemPosition); });
            await NotifyWearAppOfChange();
        }
        #endregion

        #region Icon Dialog
        private void OpenIconDialog(int position)
        {
            var fragment = new ChangeIconBottomSheet(position, IsDark);
            fragment.IconSelect += OnIconDialogIconSelected;
            fragment.UseCustomIconClick += delegate 
            {
                _customIconApplyPosition = position;
                OpenFilePicker("image/*", ResultCustomIcon);
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
                await TryCleanupCustomIcon(oldIcon);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(e.ItemPosition); });
            await NotifyWearAppOfChange();

            ((ChangeIconBottomSheet) sender).Dismiss();
        }
        #endregion

        #region Custom Icons
        private async Task SetCustomIcon(Uri uri)
        {
            CustomIcon icon;

            try
            {
                var data = await FileUtil.ReadFile(this, uri);
                icon = await CustomIcon.FromBytes(data);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            
            var auth = _authSource.Get(_customIconApplyPosition);

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

            await TryCleanupCustomIcon(oldIcon);
            
            RunOnUiThread(delegate { _authListAdapter.NotifyItemChanged(_customIconApplyPosition); });
            await NotifyWearAppOfChange();
        }

        private async Task TryCleanupCustomIcon(string icon)
        {
            if(icon != null && icon.StartsWith(CustomIcon.Prefix))
            {
                var id = icon.Substring(1);

                if(!_authSource.IsCustomIconInUse(id))
                    await _customIconSource.Delete(id);
            }
        }
        #endregion

        #region Categories
        private void OpenCategoriesDialog(int position)
        {
            var auth = _authSource.Get(position);

            if(auth == null)
                return;

            var fragment = new AssignCategoriesBottomSheet(_categorySource, position, _authSource.GetCategories(position));
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

        private void ScrollToPosition(int position)
        {
            if(position < 0 || position > _authSource.GetView().Count - 1)
                return;
            
            _authList.SmoothScrollToPosition(position);
            _appBarLayout.SetExpanded(true);
        }
        
        private void ShowDatabaseErrorDialog()
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.databaseError);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.quit, delegate
            {
                Process.KillProcess(Process.MyPid());
            });
            builder.SetCancelable(false);

            var dialog = builder.Create();
            dialog.Show();
        }
        
        private void OpenFilePicker(string mimeType, int requestCode)
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch(ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong); 
            }
        }

        private bool RequiresAuthentication()
        {
            var hasAppLock = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_appLock", false);
            
            var keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            var isDeviceSecure = Build.VERSION.SdkInt <= BuildVersionCodes.LollipopMr1
                ? keyguardManager.IsKeyguardSecure
                : keyguardManager.IsDeviceSecure;

            return hasAppLock && isDeviceSecure;
        }
        #endregion
    }
}