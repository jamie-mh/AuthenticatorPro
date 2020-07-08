using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Common;
using Android.Gms.Common.Apis;
using Android.Gms.Wearable;
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
using AuthenticatorPro.Fragment;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomAppBar;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Java.IO;
using SQLite;
using ZXing;
using ZXing.Mobile;
using Result = Android.App.Result;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using SQLiteException = SQLite.SQLiteException;
using Timer = System.Timers.Timer;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;


namespace AuthenticatorPro.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/MainActivityTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher", WindowSoftInputMode = SoftInput.AdjustPan)]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    internal class MainActivity : DayNightActivity, CapabilityClient.IOnCapabilityChangedListener
    {
        private const string WearRefreshCapability = "refresh";
        private const int PermissionCameraCode = 0;

        private const int ResultRestoreSAF = 1;
        private const int ResultBackupSAF = 2;
        private const int ResultCustomIconSAF = 3;
        private const int ResultSettingsRecreate = 4;

        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        private CoordinatorLayout _coordinatorLayout;
        private RecyclerView _authList;
        private ProgressBar _progressBar;
        private SearchView _searchView;
        private FloatingActionButton _addButton;
        private BottomAppBar _bottomAppBar;

        private LinearLayout _emptyStateLayout;
        private TextView _emptyMessageText;
        private MaterialButton _viewGuideButton;

        private AuthenticatorListAdapter _authenticatorListAdapter;
        private AuthenticatorSource _authenticatorSource;
        private CategorySource _categorySource;
        private CustomIconSource _customIconSource;

        private SQLiteAsyncConnection _connection;
        private Timer _timer;
        private DateTime _pauseTime;
        
        private Task _onceResumedTask;
        private bool _refreshOnActivityResume;
        private int _customIconApplyPosition;

        private KeyguardManager _keyguardManager;


        public MainActivity()
        {
            _pauseTime = DateTime.MinValue;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            MobileBarcodeScanner.Initialize(Application);
            
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activityMain);

            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);

            _bottomAppBar = FindViewById<BottomAppBar>(Resource.Id.bottomAppBar);
            _bottomAppBar.NavigationClick += OnBottomAppBarNavigationClick;
            _bottomAppBar.MenuItemClick += (sender, args) =>
            {
                toolbar.Menu.FindItem(Resource.Id.actionSearch).ExpandActionView();
                _authList.SmoothScrollToPosition(0);
            };

            _coordinatorLayout = FindViewById<CoordinatorLayout>(Resource.Id.coordinatorLayout);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddButtonClick;

            _authList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);
            _emptyMessageText = FindViewById<TextView>(Resource.Id.textEmptyMessage);
            _viewGuideButton = FindViewById<MaterialButton>(Resource.Id.buttonViewGuide);
            _viewGuideButton.Click += (sender, args) =>
            {
                StartActivity(typeof(GuideActivity));
            };

            _refreshOnActivityResume = false;
            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            DetectGoogleAPIsAvailability();

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var firstLaunch = prefs.GetBoolean("firstLaunch", true);

            if(firstLaunch)
                StartActivity(typeof(IntroActivity));
        }

        protected override async void OnResume()
        {
            base.OnResume();

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1 && PerformLogin())
                return;

            // Just launched
            if(_connection == null)
            {
                try
                {
                    _connection = await Database.Connect(this);
                }
                catch(SQLiteException)
                {
                    ShowDatabaseErrorDialog();
                    return;
                }

                await Init();
            }
            else if(_refreshOnActivityResume)
            {
                _refreshOnActivityResume = false;

                _authList.Visibility = ViewStates.Invisible;
                await RefreshAuthenticators();
                await _categorySource.Update();
                await _customIconSource.Update();

                // Currently visible category has been deleted
                if(_authenticatorSource.CategoryId != null &&
                   _categorySource.Categories.FirstOrDefault(c => c.Id == _authenticatorSource.CategoryId) == null)
                    await SwitchCategory(null);
            }

            CheckEmptyState();

            // Launch task that needs to wait for the activity to resume
            // Useful because an activity result is called before resume
            if(_onceResumedTask != null)
            {
                _onceResumedTask.Start();
                _onceResumedTask = null;
            }

            _timer.Start();
            Tick();

            var showBackupReminders = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_showBackupReminders", true);
           
            if(showBackupReminders)
                RemindBackup();

            await DetectWearOSCapability();

            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).AddListenerAsync(this, WearRefreshCapability);
        }

        private async Task Init()
        {
            _authenticatorSource = new AuthenticatorSource(_connection);
            await _authenticatorSource.Update();

            _categorySource = new CategorySource(_connection);
            await _categorySource.Update();
            
            _customIconSource = new CustomIconSource(_connection);
            await _customIconSource.Update();

            InitAuthenticatorList();
            await RefreshAuthenticators();

            _timer = new Timer {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += Tick;
            _timer.Start();
        }

        private void ShowDatabaseErrorDialog()
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.databaseError);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.quit, (sender, args) =>
            {
                Finish();
            });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
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
            
            _authenticatorListAdapter = new AuthenticatorListAdapter(_authenticatorSource, _customIconSource, viewMode, IsDark);

            _authenticatorListAdapter.ItemClick += OnAuthenticatorClick;
            _authenticatorListAdapter.MenuClick += OnAuthenticatorOptionsClick;
            _authenticatorListAdapter.MovementStarted += (sender, i) =>
            {
                _bottomAppBar.PerformHide();
            };
            _authenticatorListAdapter.MovementFinished += async (sender, i) =>
            {
                _bottomAppBar.PerformShow();
                await NotifyWearAppOfChange();
            };

            _authenticatorListAdapter.HasStableIds = true;

            _authList.SetAdapter(_authenticatorListAdapter);

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

            var animation =
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            var callback = new ReorderableListTouchHelperCallback(_authenticatorListAdapter, layout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);
        }

        private async Task RefreshAuthenticators(bool viewOnly = false)
        {
            if(!viewOnly)
            {
                _progressBar.Visibility = ViewStates.Visible;
                await _authenticatorSource.Update();
            }

            _authenticatorListAdapter.NotifyDataSetChanged();
            _authList.ScheduleLayoutAnimation();

            if(!viewOnly)
                _progressBar.Visibility = ViewStates.Invisible;
        }

        private void CheckEmptyState()
        {
            if(!_authenticatorSource.Authenticators.Any())
            {
                _authList.Visibility = ViewStates.Invisible;
                AnimUtil.FadeInView(_emptyStateLayout, 500, true);

                if(_authenticatorSource.CategoryId == null)
                {
                    _emptyMessageText.SetText(Resource.String.noAuthenticatorsHelp);
                    _viewGuideButton.Visibility = ViewStates.Visible;
                }
                else
                {
                    _emptyMessageText.SetText(Resource.String.noAuthenticatorsMessage);
                    _viewGuideButton.Visibility = ViewStates.Gone;
                }
            }
            else
            {
                _emptyStateLayout.Visibility = ViewStates.Invisible;
                AnimUtil.FadeInView(_authList, 100, true);
            }
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();

            if(_connection != null)
                await _connection.CloseAsync();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);

            var searchItem = menu.FindItem(Resource.Id.actionSearch);
            _searchView = (SearchView) searchItem.ActionView;
            _searchView.QueryHint = GetString(Resource.String.search);

            _searchView.QueryTextChange += (sender, e) =>
            {
                var oldSearch = _authenticatorSource.Search;

                _authenticatorSource.SetSearch(e.NewText);
                _authenticatorListAdapter.NotifyDataSetChanged();

                if(e.NewText == "" && !String.IsNullOrEmpty(oldSearch))
                    searchItem.CollapseActionView();
            };

            _searchView.Close += (sender, e) =>
            {
                searchItem.CollapseActionView();
                _authenticatorSource.SetSearch(null);
            };

            return base.OnCreateOptionsMenu(menu);
        }

        private void OnBottomAppBarNavigationClick(object sender, Toolbar.NavigationClickEventArgs e)
        {
            var fragment = new MainMenuBottomSheet(_categorySource, _authenticatorSource.CategoryId);
            fragment.CategoryClick += async (s, id) =>
            {
                await SwitchCategory(id);
                fragment.Dismiss();
            };

            fragment.BackupClick += (sender, e) =>
            {
                if(!_authenticatorSource.Authenticators.Any())
                {
                    ShowSnackbar(Resource.String.noAuthenticators, Snackbar.LengthShort);
                    return;
                }

                StartBackupSaveActivity();
            };

            fragment.ManageCategoriesClick += (sender, e) =>
            {
                _refreshOnActivityResume = true;
                StartActivity(typeof(ManageCategoriesActivity));
            };
            fragment.SettingsClick += (sender, e) =>
            {
                _refreshOnActivityResume = true;
                StartActivityForResult(typeof(SettingsActivity), ResultSettingsRecreate);
            };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void StartBackupSaveActivity()
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, $"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

            StartActivityForResult(intent, ResultBackupSAF);
        }

        private async Task SwitchCategory(string id)
        {
            if(id == _authenticatorSource.CategoryId)
                return;

            if(id == null)
            {
                _authenticatorSource.SetCategory(null);
                SupportActionBar.Title = GetString(Resource.String.categoryAll);
            }
            else
            {
                var category = _categorySource.Categories.First(c => c.Id == id);
                _authenticatorSource.SetCategory(id);
                SupportActionBar.Title = category.Name;
            }

            await RefreshAuthenticators(true);

            _authList.Visibility = ViewStates.Invisible;
            CheckEmptyState();
        }

        protected override async void OnPause()
        {
            base.OnPause();

            _timer?.Stop();
            _pauseTime = DateTime.Now;

            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).RemoveListenerAsync(this, WearRefreshCapability);
        }

        private bool PerformLogin()
        {
            var authRequired = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_appLock", false);

            var isDeviceSecure = Build.VERSION.SdkInt <= BuildVersionCodes.LollipopMr1
                ? _keyguardManager.IsKeyguardSecure
                : _keyguardManager.IsDeviceSecure;

            if(authRequired && isDeviceSecure)
            {
                StartActivity(typeof(LoginActivity));
                return true;
            }

            return false;
        }

        public override async void OnBackPressed()
        {
            if(!_searchView.Iconified)
            {
                _searchView.Iconified = true;
                return;
            }

            if(_authenticatorSource.CategoryId != null)
            {
                await SwitchCategory(null);
                return;
            }

            Finish();
        }

        private void Tick(object sender = null, ElapsedEventArgs e = null)
        {
            if(_authenticatorSource == null)
                return;

            for(var i = 0; i < _authenticatorSource.Authenticators.Count; ++i)
            {
                var position = i;
                RunOnUiThread(() => { _authenticatorListAdapter.NotifyItemChanged(position, true); });
            }
        }

        private void OnAuthenticatorClick(object sender, int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("code", auth.GetCode());
            clipboard.PrimaryClip = clip;

            ShowSnackbar(Resource.String.copiedToClipboard, Snackbar.LengthShort);
        }

        private void OnAuthenticatorOptionsClick(object sender, int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var fragment = new EditMenuBottomSheet(auth.Type, auth.Counter);
            fragment.ClickRename += (s, e) => { OpenRenameDialog(position); };
            fragment.ClickChangeIcon += (s, e) => { OpenIconDialog(position); };
            fragment.ClickAssignCategories += (s, e) => { OpenCategoriesDialog(position); };
            fragment.ClickDelete += (s, e) => { OpenDeleteDialog(position); };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OpenDeleteDialog(int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, async (sender, args) =>
            {
                var auth = _authenticatorSource.Authenticators[position];
                await TryCleanupCustomIcon(auth.Icon);
                
                await _authenticatorSource.Delete(position);
                _authenticatorListAdapter.NotifyItemRemoved(position);
                
                await NotifyWearAppOfChange();
                CheckEmptyState();
            });
            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        private void OnAddButtonClick(object sender, EventArgs e)
        {
            var fragment = new AddMenuBottomSheet();
            fragment.ClickQrCode += OpenQRCodeScanner;
            fragment.ClickEnterKey += OpenAddDialog;
            fragment.ClickRestore += (s, e) =>
            {
                var intent = new Intent(Intent.ActionOpenDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("application/octet-stream");
                StartActivityForResult(intent, ResultRestoreSAF);
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        public override async void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionCameraCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    await ScanQRCode();
                else
                    ShowSnackbar(Resource.String.cameraPermissionError, Snackbar.LengthShort);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async Task ScanQRCode()
        {
            var options = new MobileBarcodeScanningOptions {
                PossibleFormats = new List<BarcodeFormat> {
                    BarcodeFormat.QR_CODE
                },
                TryHarder = true,
                AutoRotate = true
            };

            var scanner = new MobileBarcodeScanner();
            var result = await scanner.Scan(options);

            if(result == null)
                return;

            Authenticator auth;

            try
            {
                auth = Authenticator.FromKeyUri(result.Text);
            }
            catch
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
                return;
            }

            if(_authenticatorSource.IsDuplicate(auth))
            {
                ShowSnackbar(Resource.String.duplicateAuthenticator, Snackbar.LengthShort);
                return;
            }

            await _connection.InsertAsync(auth);

            if(_authenticatorSource.CategoryId != null)
                await _authenticatorSource.AddToCategory(auth.Secret, _authenticatorSource.CategoryId);
            
            await _authenticatorSource.Update();
            CheckEmptyState();

            var position = _authenticatorSource.GetPosition(auth.Secret);
            _authenticatorListAdapter.NotifyItemInserted(position);
            _authList.SmoothScrollToPosition(position);
            await NotifyWearAppOfChange();

            PreferenceManager.GetDefaultSharedPreferences(this)
                .Edit()
                .PutBoolean("needsBackup", true)
                .Commit();
        }

        private async void OpenQRCodeScanner(object sender, EventArgs e)
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            else
                await ScanQRCode();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(resultCode != Result.Ok)
                return;

            _onceResumedTask = requestCode switch {
                ResultRestoreSAF => new Task(async () =>
                {
                    await BeginRestore(intent.Data);
                }),
                ResultBackupSAF => new Task(() =>
                {
                    BeginBackup(intent.Data);
                }),
                ResultCustomIconSAF => new Task(async () =>
                {
                    await SetCustomIcon(intent.Data);
                }),
                ResultSettingsRecreate => new Task(() =>
                {
                    RunOnUiThread(Recreate);
                }),
                _ => _onceResumedTask
            };
        }

        /*
         *  Restore
         */

        private async Task BeginRestore(Uri uri)
        {
            MemoryStream memoryStream = null;
            Stream stream = null;
            byte[] fileData;

            try
            {
                stream = ContentResolver.OpenInputStream(uri);
                memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            finally
            {
                memoryStream?.Close();
                stream?.Close();
            }

            if(fileData.Length == 0)
            {
                ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                return;
            }

            async Task TryRestore(string password, BackupPasswordBottomSheet sheet)
            {
                int authCount, categoryCount;

                try
                {
                    (authCount, categoryCount) = await DoRestore(fileData, password);
                }
                catch(InvalidAuthenticatorException)
                {
                    sheet?.Dismiss();
                    ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                    return;
                }
                catch(Exception)
                {
                    if(sheet != null)
                        sheet.Error = GetString(Resource.String.restoreError);
                    else
                        ShowSnackbar(Resource.String.restoreError, Snackbar.LengthShort);

                    return;
                }

                sheet?.Dismiss();

                await _customIconSource.Update();
                await _authenticatorSource.Update();

                // This is required because we're probably not running on the main thread
                // as the method was called from a task
                RunOnUiThread(async () =>
                {
                    CheckEmptyState();
                    await RefreshAuthenticators(true);
                });

                await _categorySource.Update();

                var message = String.Format(GetString(Resource.String.restoredFromBackup), authCount, categoryCount);
                ShowSnackbar(message, Snackbar.LengthLong);

                await NotifyWearAppOfChange();
            }

            // Open and closed curly brace (file is not encrypted)
            if(fileData[0] == 0x7b && fileData[^1] == 0x7d)
            {
                await TryRestore(null, null);
                return;
            }

            var fragment = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Restore);
            fragment.PasswordEntered += async (sender, password) =>
            {
                await TryRestore(password, fragment);
            };

            fragment.Cancel += (s, e) =>
            {
                fragment.Dismiss();
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task<Tuple<int, int>> DoRestore(byte[] fileData, string password)
        {
            var backup = Backup.FromBytes(fileData, password);

            if(backup.Authenticators == null)
                throw new InvalidAuthenticatorException();

            var authsInserted = 0;
            var categoriesInserted = 0;
            
            foreach(var auth in backup.Authenticators.Where(auth => !_authenticatorSource.IsDuplicate(auth)))
            {
                auth.Validate();
                await _connection.InsertAsync(auth);
                authsInserted++;
            }

            foreach(var category in backup.Categories.Where(category => !_categorySource.IsDuplicate(category)))
            {
                await _connection.InsertAsync(category);
                categoriesInserted++;
            }

            foreach(var binding in backup.AuthenticatorCategories.Where(binding => !_authenticatorSource.IsDuplicateCategoryBinding(binding)))
                await _connection.InsertAsync(binding);

            // Older backups might not have custom icons
            if(backup.CustomIcons != null)
            {
                foreach(var icon in backup.CustomIcons.Where(i => !_customIconSource.IsDuplicate(i.Id)))
                    await _connection.InsertAsync(icon);
            }

            return new Tuple<int, int>(authsInserted, categoriesInserted);
        }

        /*
         *  Backup
         */

        private void BeginBackup(Uri uri)
        {
            var fragment = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Backup);
            fragment.PasswordEntered += async (sender, password) =>
            {
                try
                {
                    await DoBackup(uri, password);
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

        private async Task DoBackup(Uri uri, string password)
        {
            var backup = new Backup(
                _authenticatorSource.Authenticators,
                _categorySource.Categories,
                _authenticatorSource.CategoryBindings,
                _customIconSource.Icons
            );

            var dataToWrite = backup.ToBytes(password);
    
            // This is the only way of reliably writing files using SAF on Xamarin.
            // A file output stream will usually create 0 byte files on virtual storage such as Google Drive
            var output = ContentResolver.OpenOutputStream(uri, "rwt");
            var dataStream = new DataOutputStream(output);

            try
            {
                await dataStream.WriteAsync(dataToWrite);
                await dataStream.FlushAsync();
            }
            finally
            {
                dataStream.Close();
                output.Close();
            }
        }

        /*
         *  Add Dialog
         */

        private void OpenAddDialog(object sender, EventArgs e)
        {
            var fragment = new AddAuthenticatorBottomSheet();
            fragment.Add += OnAddDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAddDialogSubmit(object sender, Authenticator auth)
        {
            var dialog = (AddAuthenticatorBottomSheet) sender;

            if(_authenticatorSource.IsDuplicate(auth))
            {
                dialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }

            await _connection.InsertAsync(auth);

            if(_authenticatorSource.CategoryId != null)
                await _authenticatorSource.AddToCategory(auth.Secret, _authenticatorSource.CategoryId);

            await _authenticatorSource.Update();
            CheckEmptyState();

            var position = _authenticatorSource.GetPosition(auth.Secret);
            _authenticatorListAdapter.NotifyItemInserted(position);
            _authList.SmoothScrollToPosition(position);
            await NotifyWearAppOfChange();

            PreferenceManager.GetDefaultSharedPreferences(this)
                .Edit()
                .PutBoolean("needsBackup", true)
                .Commit();
            
            dialog.Dismiss();
        }

        /*
         *  Rename Dialog
         */

        private void OpenRenameDialog(int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var fragment = new RenameAuthenticatorBottomSheet(position, auth.Issuer, auth.Username);
            fragment.Rename += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, RenameAuthenticatorBottomSheet.RenameEventArgs e)
        {
             await _authenticatorSource.Rename(e.ItemPosition, e.Issuer, e.Username);
            _authenticatorListAdapter.NotifyItemChanged(e.ItemPosition);
            await NotifyWearAppOfChange();
        }

        /*
         *  Icon Dialog
         */

        private void OpenIconDialog(int position)
        {
            var fragment = new ChangeIconBottomSheet(position, IsDark);
            fragment.IconSelect += OnIconDialogIconSelected;
            fragment.UseCustomIconClick += (sender, args) =>
            {
                _customIconApplyPosition = position;
                StartCustomIconPicker();
            };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void StartCustomIconPicker()
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("image/*");
            StartActivityForResult(intent, ResultCustomIconSAF);
        }

        private async void OnIconDialogIconSelected(object sender, ChangeIconBottomSheet.IconSelectedEventArgs e)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(e.ItemPosition);

            if(auth == null)
                return;

            await TryCleanupCustomIcon(auth.Icon);
            auth.Icon = e.Icon;

            await _connection.UpdateAsync(auth);
            _authenticatorListAdapter.NotifyItemChanged(e.ItemPosition);
            await NotifyWearAppOfChange();

            ((ChangeIconBottomSheet) sender).Dismiss();
        }
        
        /*
         *  Custom Icons
         */

        private async Task SetCustomIcon(Uri uri)
        {
            MemoryStream memoryStream = null;
            Stream stream = null;
            CustomIcon icon;

            try
            {
                stream = ContentResolver.OpenInputStream(uri);
                memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                
                var fileData = memoryStream.ToArray();
                icon = await CustomIcon.FromBytes(fileData);
            }
            catch(Exception)
            {
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                return;
            }
            finally
            {
                memoryStream?.Close();
                stream?.Close();
            }
            
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(_customIconApplyPosition);

            if(auth == null || auth.Icon == CustomIcon.Prefix + icon.Id)
                return;
            
            if(!_customIconSource.IsDuplicate(icon.Id))
                await _connection.InsertAsync(icon);
            
            await TryCleanupCustomIcon(auth.Icon);
            
            auth.Icon = CustomIcon.Prefix + icon.Id;
            await _connection.UpdateAsync(auth);

            await _customIconSource.Update();
            RunOnUiThread(() =>
            {
                _authenticatorListAdapter.NotifyItemChanged(_customIconApplyPosition); 
            });

            await NotifyWearAppOfChange();
        }

        private async Task TryCleanupCustomIcon(string icon)
        {
            if(icon.StartsWith(CustomIcon.Prefix))
            {
                var id = icon.Substring(1);

                if(_authenticatorSource.CountCustomIconUses(id) == 1)
                    await _customIconSource.Delete(id);
            }
        }
        
        /*
         *  Categories Dialog
         */

        private void OpenCategoriesDialog(int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var fragment = new AssignCategoriesBottomSheet(_categorySource, position, _authenticatorSource.GetCategories(position));
            fragment.CategoryClick += OnCategoriesDialogCategoryClick;
            fragment.ManageCategoriesClick += (sender, e) =>
            {
                _refreshOnActivityResume = true;
                StartActivity(typeof(ManageCategoriesActivity));
                fragment.Dismiss();
            };
            fragment.Close += OnCategoriesDialogClose;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnCategoriesDialogClose(object sender, EventArgs e)
        {
            if(_authenticatorSource.CategoryId != null)
            {
                _authenticatorSource.UpdateView();
                _authenticatorListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            }
        }

        private async void OnCategoriesDialogCategoryClick(object sender, AssignCategoriesBottomSheet.CategoryClickedEventArgs e)
        {
            var categoryId = _categorySource.Categories[e.CategoryPosition].Id;
            var authSecret = _authenticatorSource.Authenticators[e.ItemPosition].Secret;

            if(e.IsChecked)
                await _authenticatorSource.AddToCategory(authSecret, categoryId);
            else
                await _authenticatorSource.RemoveFromCategory(authSecret, categoryId);
        }

        /*
         *  Wear OS
         */

        public async void OnCapabilityChanged(ICapabilityInfo capabilityInfo)
        {
            await DetectWearOSCapability();
        }

        private void DetectGoogleAPIsAvailability()
        {
            _areGoogleAPIsAvailable = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this) == 
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

        /*
         *  Misc
         */

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

        private void RemindBackup()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var needsBackup = prefs.GetBoolean("needsBackup", false);

            if(!needsBackup)
                return;
            
            var snackbar = Snackbar.Make(_coordinatorLayout, Resource.String.backupReminder, Snackbar.LengthLong);
            snackbar.SetAnchorView(_addButton);
            snackbar.SetAction(Resource.String.backupNow, view =>
            {
                StartBackupSaveActivity(); 
            });
            
            var callback = new SnackbarCallback();
            callback.Dismiss += (sender, e) =>
            {
                if(e == Snackbar.Callback.DismissEventSwipe)
                    prefs.Edit().PutBoolean("needsBackup", false).Commit();
            };

            snackbar.AddCallback(callback);
            snackbar.Show();
        }
    }
}