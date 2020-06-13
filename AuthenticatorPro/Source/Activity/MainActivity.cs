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
using Android.Provider;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialog;
using AuthenticatorPro.Fragment;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Util;
using Google.Android.Material.BottomAppBar;
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

        private const int RestoreStorageAccessFrameworkCode = 1;
        private const int BackupStorageAccessFrameworkCode = 2;

        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        private Task _onceResumedTask;

        private CoordinatorLayout _coordinatorLayout;
        private LinearLayout _emptyStateLayout;
        private RecyclerView _authList;
        private ProgressBar _progressBar;
        private SearchView _searchView;
        private FloatingActionButton _addButton;

        private AuthenticatorListAdapter _authenticatorListAdapter;
        private AuthenticatorSource _authenticatorSource;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private Timer _timer;
        private DateTime _pauseTime;
        private bool _isChildActivityOpen;

        private ISubMenu _categoriesMenu;
        private KeyguardManager _keyguardManager;
        private MobileBarcodeScanner _barcodeScanner;
        private IdleActionBarDrawerToggle _actionBarDrawerToggle;


        public MainActivity()
        {
            _pauseTime = DateTime.MinValue;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activityMain);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);

            var bottomAppBar = FindViewById<BottomAppBar>(Resource.Id.bottomAppBar);
            bottomAppBar.NavigationClick += OnBottomAppBarNavigationClick;
            bottomAppBar.MenuItemClick += (sender, args) =>
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

            _isChildActivityOpen = false;
            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            DetectGoogleAPIsAvailability();

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var firstLaunch = prefs.GetBoolean("firstLaunch", true);

            if(firstLaunch)
                StartChildActivity(typeof(IntroActivity));
        }

        protected override async void OnResume()
        {
            base.OnResume();

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1 && PerformLogin())
                return;

            // Just launched
            if(_connection == null)
            {
                await Init();
            }
            else if(_isChildActivityOpen)
            {
                // TODO: reconnect to db when encryption changed
                if(RequiresRecreate())
                {
                    Recreate();
                    return;
                }

                await RefreshAuthenticators();
                await _categorySource.Update();

                // Currently visible category has been deleted
                if(_authenticatorSource.CategoryId != null &&
                   _categorySource.Categories.FirstOrDefault(c => c.Id == _authenticatorSource.CategoryId) == null)
                    await SwitchCategory(-1);
            }

            CheckEmptyState();

            _isChildActivityOpen = false;
            _timer.Start();
            Tick();

            // Launch task that needs to wait for the activity to resume
            // Useful because an activity result is called before resume
            if(_onceResumedTask != null)
            {
                _onceResumedTask.Start();
                _onceResumedTask = null;
            }

            await DetectWearOSCapability();

            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).AddListenerAsync(this, WearRefreshCapability);
        }

        private void StartChildActivity(Type type)
        {
            _isChildActivityOpen = true;
            StartActivity(type);
        }

        private bool RequiresRecreate()
        {
            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);

            var isCompact = prefs.GetBoolean("pref_compactMode", false);
            if(isCompact != _authenticatorListAdapter.IsCompact)
                return true;

            return false;
        }

        private async Task Init()
        {
            try
            {
                _connection = await Database.Connect(this);

                _authenticatorSource = new AuthenticatorSource(_connection);
                _authenticatorSource.Update();

                _categorySource = new CategorySource(_connection);
                await _categorySource.Update();

                InitAuthenticatorList();
                await RefreshAuthenticators();

                CreateTimer();
            }
            catch(SQLiteException)
            {
                ShowDatabaseErrorDialog();
            }
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
            var isCompact = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_compactMode", false);

            _authenticatorListAdapter = new AuthenticatorListAdapter(_authenticatorSource, IsDark, isCompact);

            _authenticatorListAdapter.ItemClick += OnAuthenticatorClick;
            _authenticatorListAdapter.MenuClick += OnAuthenticatorOptionsClick;
            _authenticatorListAdapter.MovementFinished += async (sender, i) =>
            {
                await NotifyWearAppOfChange();
            };

            _authenticatorListAdapter.HasStableIds = true;

            _authList.SetAdapter(_authenticatorListAdapter);
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(20);

            var animation =
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            var useGrid = IsTablet();
            var layout = new AnimatedGridLayoutManager(this, useGrid ? 2 : 1);
            _authList.SetLayoutManager(layout);

            var callback = new ReorderableListTouchHelperCallback(_authenticatorListAdapter, useGrid);
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
                _progressBar.Visibility = ViewStates.Gone;
        }

        private void CheckEmptyState()
        {
            if(!_authenticatorSource.Authenticators.Any())
            {
                _authList.Visibility = ViewStates.Gone;
                AnimUtil.FadeInView(_emptyStateLayout, 500, true);
            }
            else
            {
                _emptyStateLayout.Visibility = ViewStates.Gone;
                AnimUtil.FadeInView(_authList, 100, true);
            }
        }

        private void CreateTimer()
        {
            _timer = new Timer {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += Tick;
            _timer.Start();
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
            fragment.CategoryClick += async (s, pos) =>
            {
                await SwitchCategory(pos);
                fragment.Dismiss();
            };

            fragment.BackupClick += (sender, e) =>
            {
                if(!_authenticatorSource.Authenticators.Any())
                {
                    ShowSnackbar(Resource.String.noAuthenticators, Snackbar.LengthShort);
                    return;
                }

                var intent = new Intent(Intent.ActionCreateDocument);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("application/octet-stream");
                intent.PutExtra(Intent.ExtraTitle, $"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

                StartActivityForResult(intent, BackupStorageAccessFrameworkCode);
                _isChildActivityOpen = true;
            };

            fragment.ManageCategoriesClick += (sender, e) => { StartChildActivity(typeof(ManageCategoriesActivity)); };
            fragment.SettingsClick += (sender, e) => { StartChildActivity(typeof(SettingsActivity)); };
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async Task SwitchCategory(int position)
        {
            if(position < 0)
            {
                _authenticatorSource.SetCategory(null);
                SupportActionBar.Title = GetString(Resource.String.categoryAll);
            }
            else
            {
                var category = _categorySource.Categories[position];
                _authenticatorSource.SetCategory(category.Id);
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

            AnimUtil.FadeOutView(_authList, 500);

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
                StartChildActivity(typeof(LoginActivity));
                return true;
            }

            return false;
        }

        public override async void OnBackPressed()
        {
            _barcodeScanner.Cancel();

            if(!_searchView.Iconified)
            {
                _searchView.OnActionViewCollapsed();
                _searchView.Iconified = true;
                return;
            }

            if(_authenticatorSource.CategoryId != null)
            {
                await SwitchCategory(-1);
                return;
            }

            base.OnBackPressed();
        }

        private void Tick(object sender = null, ElapsedEventArgs e = null)
        {
            if(_authenticatorSource == null)
                return;

            for(var i = 0; i < _authenticatorSource.Authenticators.Count; ++i)
            {
                var auth = _authenticatorSource.Authenticators[i];
                var position = i;

                switch(auth.Type)
                {
                    case AuthenticatorType.Totp when auth.TimeRenew > DateTime.Now:
                    case AuthenticatorType.Hotp when auth.TimeRenew < DateTime.Now:
                        RunOnUiThread(() => { _authenticatorListAdapter.NotifyItemChanged(position, true); });
                        break;

                    case AuthenticatorType.Totp:
                        RunOnUiThread(() => { _authenticatorListAdapter.NotifyItemChanged(position); });
                        break;
                }
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

        private bool IsTablet()
        {
            var display = WindowManager.DefaultDisplay;
            var displayMetrics = new DisplayMetrics();
            display.GetMetrics(displayMetrics);

            var wInches = displayMetrics.WidthPixels / (double) displayMetrics.DensityDpi;
            var hInches = displayMetrics.HeightPixels / (double) displayMetrics.DensityDpi;

            var screenDiagonal = Math.Sqrt(Math.Pow(wInches, 2) + Math.Pow(hInches, 2));
            return screenDiagonal >= 7.0;
        }

        private void OpenDeleteDialog(int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, async (sender, args) =>
            {
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
                StartActivityForResult(intent, RestoreStorageAccessFrameworkCode);
                _isChildActivityOpen = true;
            };

            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionCameraCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    ScanQRCode();
                else
                    ShowSnackbar(Resource.String.cameraPermissionError, Snackbar.LengthShort);
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private async void ScanQRCode()
        {
            var options = new MobileBarcodeScanningOptions {
                PossibleFormats = new List<BarcodeFormat> {
                    BarcodeFormat.QR_CODE
                }
            };

            var result = await _barcodeScanner.Scan(options);

            if(result == null)
                return;

            try
            {
                var auth = Authenticator.FromKeyUri(result.Text);

                if(_authenticatorSource.IsDuplicate(auth))
                {
                    ShowSnackbar(Resource.String.duplicateAuthenticator, Snackbar.LengthShort);
                    return;
                }

                await _connection.InsertAsync(auth);
                await _authenticatorSource.Update();

                CheckEmptyState();
                var position = _authenticatorSource.GetPosition(auth.Secret);
                _authenticatorListAdapter.NotifyItemInserted(position);
                _authList.SmoothScrollToPosition(position);
                await NotifyWearAppOfChange();
            }
            catch
            {
                ShowSnackbar(Resource.String.qrCodeFormatError, Snackbar.LengthShort);
            }
        }

        private void OpenQRCodeScanner(object sender, EventArgs e)
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            else
                ScanQRCode();
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(resultCode != Result.Ok)
                return;

            _onceResumedTask = requestCode switch {
                RestoreStorageAccessFrameworkCode => new Task(async () =>
                {
                    await BeginRestore(intent.Data);
                }),
                BackupStorageAccessFrameworkCode => new Task(async () =>
                {
                    await BeginBackup(intent.Data);
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
                try
                {
                    var (authCount, categoryCount) = await DoRestore(fileData, password);
                    sheet?.Dismiss();

                    // This is required because we're probably not running on the main thread
                    // as the method was called from a task
                    RunOnUiThread(async () =>
                    {
                        await RefreshAuthenticators();
                        CheckEmptyState();
                    });

                    await _categorySource.Update();

                    var message = String.Format(GetString(Resource.String.restoredFromBackup), authCount, categoryCount);
                    ShowSnackbar(message, Snackbar.LengthLong);
                }

                catch(InvalidAuthenticatorException)
                {
                    sheet?.Dismiss();
                    ShowSnackbar(Resource.String.invalidFileError, Snackbar.LengthShort);
                }
                catch(Exception e)
                {
                    if(sheet != null)
                        sheet.Error = GetString(Resource.String.restoreError);
                    else
                        ShowSnackbar(Resource.String.restoreError, Snackbar.LengthShort);
                }
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
            {
                await _connection.InsertAsync(binding);
            }

            return new Tuple<int, int>(authsInserted, categoriesInserted);
        }

        /*
         *  Backup
         */

        private async Task BeginBackup(Uri uri)
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

                ((BackupPasswordBottomSheet) sender).Dismiss();
                ShowSnackbar(Resource.String.saveSuccess, Snackbar.LengthLong);
            };

            fragment.Cancel += async (sender, _) =>
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
                _authenticatorSource.CategoryBindings
            );

            var dataToWrite = backup.ToBytes(password);

            var output = ContentResolver.OpenOutputStream(uri);
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
            await _authenticatorSource.Update();

            CheckEmptyState();
            var position = _authenticatorSource.GetPosition(auth.Secret);
            _authenticatorListAdapter.NotifyItemInserted(position);
            _authList.SmoothScrollToPosition(position);
            await NotifyWearAppOfChange();

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
            var fragment = new ChangeIconDialog(position, IsDark);
            fragment.IconSelected += OnIconDialogIconSelected;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnIconDialogIconSelected(object sender, ChangeIconDialog.IconSelectedEventArgs e)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(e.ItemPosition);

            if(auth == null)
                return;

            auth.Icon = e.Icon;

            await _connection.UpdateAsync(auth);
            _authenticatorListAdapter.NotifyItemChanged(e.ItemPosition);
            await NotifyWearAppOfChange();

            ((ChangeIconDialog) sender).Dismiss();
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
                StartChildActivity(typeof(ManageCategoriesActivity));
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

        private void OnCategoriesDialogCategoryClick(object sender, AssignCategoriesBottomSheet.CategoryClickedEventArgs e)
        {
            var categoryId = _categorySource.Categories[e.CategoryPosition].Id;

            if(e.IsChecked)
                _authenticatorSource.AddToCategory(e.ItemPosition, categoryId);
            else
                _authenticatorSource.RemoveFromCategory(e.ItemPosition, categoryId);
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
    }
}