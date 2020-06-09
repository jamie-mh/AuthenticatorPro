using System;
using System.Collections.Generic;
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
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.DrawerLayout.Widget;
using AndroidX.Preference;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialog;
using AuthenticatorPro.Fragment;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Util;
using AuthenticatorPro.Util;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using SQLite;
using ZXing;
using ZXing.Mobile;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using SQLiteException = SQLite.SQLiteException;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activity
{
    [Activity(Label = "@string/displayName", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    internal class MainActivity : LightDarkActivity, CapabilityClient.IOnCapabilityChangedListener
    {
        private const string WearRefreshCapability = "refresh";
        private const int PermissionCameraCode = 0;

        private bool _areGoogleAPIsAvailable;
        private bool _hasWearAPIs;
        private bool _hasWearCompanion;

        private NavigationView _navigationView;
        private DrawerLayout _drawerLayout;
        private LinearLayout _emptyStateLayout;
        private RecyclerView _authList;
        private ProgressBar _progressBar;
        private SearchView _searchView;
        private FloatingActionButton _addButton;

        private AddAuthenticatorDialog _addDialog;
        private ChooseCategoriesDialog _categoriesDialog;
        private RenameAuthenticatorDialog _renameDialog;
        private IconDialog _iconDialog;

        private AuthenticatorListAdapter _authenticatorListAdapter;
        private AuthenticatorSource _authenticatorSource;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private Timer _authTimer;
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

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _progressBar = FindViewById<ProgressBar>(Resource.Id.activityMain_progressBar);

            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.activityMain_drawerLayout);
            _navigationView = FindViewById<NavigationView>(Resource.Id.activityMain_navView);
            _navigationView.NavigationItemSelected += OnDrawerItemSelected;

            _actionBarDrawerToggle = new IdleActionBarDrawerToggle(this, _drawerLayout, toolbar,
                Resource.String.appName, Resource.String.appName);
            _drawerLayout.AddDrawerListener(_actionBarDrawerToggle);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _addButton.Click += OnAddButtonClick;

            _authList = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.activityMain_emptyState);

            _isChildActivityOpen = false;
            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            DetectGoogleAPIsAvailability();
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            _actionBarDrawerToggle.SyncState();
        }

        protected override async void OnResume()
        {
            base.OnResume();

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var firstLaunch = prefs.GetBoolean("firstLaunch", true);

            if(firstLaunch)
            {
                StartChildActivity(typeof(IntroActivity));
                return;
            }

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1 && PerformLogin())
                return;

            // Just launched
            if(_connection == null)
                await Init();
            else if(_isChildActivityOpen)
            {
                var isCompact = prefs.GetBoolean("pref_compactMode", false);
                if(isCompact != _authenticatorListAdapter.IsCompact)
                {
                    Recreate();
                    return;
                }

                await RefreshAuthenticators();
                await RefreshCategories();

                // Currently visible category has been deleted
                if(_authenticatorSource.CategoryId != null &&
                   _categorySource.Categories.FirstOrDefault(c => c.Id == _authenticatorSource.CategoryId) == null)
                    await SwitchCategory(-1);
            }

            _isChildActivityOpen = false;
            _authTimer.Start();
            Tick();

            await DetectWearOSCapability();

            if(_hasWearAPIs)
                await WearableClass.GetCapabilityClient(this).AddListenerAsync(this, WearRefreshCapability);
        }

        private void StartChildActivity(Type type)
        {
            _isChildActivityOpen = true;
            StartActivity(type);
        }

        private async Task Init()
        {
            try
            {
                _connection = await Database.Connect(this);

                InitCategoryList();
                InitAuthenticatorList();

                await RefreshCategories();
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
            var builder = new AlertDialog.Builder(this);
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
            _authenticatorSource = new AuthenticatorSource(_connection);

            var isCompact = PreferenceManager.GetDefaultSharedPreferences(this)
                .GetBoolean("pref_compactMode", false);

            _authenticatorListAdapter = new AuthenticatorListAdapter(_authenticatorSource, IsDark, isCompact);

            _authenticatorListAdapter.ItemClick += OnItemClick;
            _authenticatorListAdapter.ItemOptionsClick += OnItemOptionsClick;
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

        private async Task RefreshAuthenticators(bool updateSource = true)
        {
            _progressBar.Visibility = ViewStates.Visible;

            if(updateSource)
                await _authenticatorSource.Update();

            _authenticatorListAdapter.NotifyDataSetChanged();
            CheckEmptyState();
            _authList.ScheduleLayoutAnimation();
            _progressBar.Visibility = ViewStates.Invisible;
        }

        private void InitCategoryList()
        {
            _categoriesMenu =
                _navigationView.Menu.AddSubMenu(Menu.None, Menu.None, Menu.None, Resource.String.categories);
            _categoriesMenu.SetGroupCheckable(0, true, true);

            _categorySource = new CategorySource(_connection);
        }

        private async Task RefreshCategories()
        {
            await _categorySource.Update();
            _categoriesMenu.Clear();

            var allItem = _categoriesMenu.Add(Menu.None, -1, Menu.None, Resource.String.categoryAll);
            allItem.SetChecked(true);

            for(var i = 0; i < _categorySource.Categories.Count; ++i)
                _categoriesMenu.Add(0, i, i, _categorySource.Categories[i].Name);
        }

        private void CheckEmptyState()
        {
            if(!_authenticatorSource.Authenticators.Any())
            {
                _authList.Visibility = ViewStates.Gone;
                AnimUtil.FadeInView(_emptyStateLayout, 500);
            }
            else
            {
                _emptyStateLayout.Visibility = ViewStates.Invisible;
                _authList.Visibility = ViewStates.Visible;
            }
        }

        private void CreateTimer()
        {
            _authTimer = new Timer {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _authTimer.Elapsed += Tick;
            _authTimer.Start();
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
                _authenticatorSource.SetSearch(e.NewText);
                _authenticatorListAdapter.NotifyDataSetChanged();
            };

            _searchView.Close += (sender, e) =>
            {
                _authenticatorSource.SetSearch("");
            };

            return base.OnCreateOptionsMenu(menu);
        }

        private void OnDrawerItemSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            _actionBarDrawerToggle.IdleAction = e.MenuItem.ItemId switch {
                Resource.Id.drawerSettings => () => { StartChildActivity(typeof(SettingsActivity)); },
                Resource.Id.drawerEditCategories => () => { StartChildActivity(typeof(EditCategoriesActivity)); },
                Resource.Id.drawerRestore => () => { StartChildActivity(typeof(RestoreActivity)); },
                Resource.Id.drawerBackup => () => { StartChildActivity(typeof(BackupActivity)); },
                _ => async () =>
                {
                    var position = e.MenuItem.ItemId;
                    await SwitchCategory(position);
                }
            };

            _drawerLayout.CloseDrawers();
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

            await RefreshAuthenticators(false);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return _actionBarDrawerToggle.OnOptionsItemSelected(item) || base.OnOptionsItemSelected(item);
        }

        protected override async void OnPause()
        {
            base.OnPause();

            _authTimer?.Stop();
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
                StartChildActivity(typeof(LoginActivity));
                return true;
            }

            return false;
        }

        public override void OnBackPressed()
        {
            _barcodeScanner.Cancel();

            if(_searchView.Iconified)
                base.OnBackPressed();
            else
            {
                _searchView.OnActionViewCollapsed();
                _searchView.Iconified = true;
            }
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

        private void OnItemClick(object sender, int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("code", auth.GetCode());
            clipboard.PrimaryClip = clip;

            Toast.MakeText(this, Resource.String.copiedToClipboard, ToastLength.Short).Show();
        }

        private void OnItemOptionsClick(object sender, int position)
        {
            if(position < 0 || position >= _authenticatorSource.Authenticators.Count)
                return;

            var builder = new AlertDialog.Builder(this);
            builder.SetItems(Resource.Array.authContextMenu, (alertSender, args) =>
            {
                switch(args.Which)
                {
                    case 0:
                        OpenRenameDialog(position);
                        break;

                    case 1:
                        OpenIconDialog(position);
                        break;

                    case 2:
                        OpenCategoriesDialog(position);
                        break;

                    case 3:
                        OpenDeleteDialog(position);
                        break;
                }
            });

            var dialog = builder.Create();
            dialog.Show();
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
            var builder = new AlertDialog.Builder(this);
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
            var fragment = new AddBottomSheetDialogFragment();
            fragment.ClickQrCode += OpenQRCodeScanner;
            fragment.ClickEnterKey += OpenAddDialog;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionCameraCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    ScanQRCode();
                else
                    Toast.MakeText(this, Resource.String.cameraPermissionError, ToastLength.Short).Show();
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
                    Toast.MakeText(this, Resource.String.duplicateAuthenticator, ToastLength.Short).Show();
                    return;
                }

                await _connection.InsertAsync(auth);
                await _authenticatorSource.Update();

                CheckEmptyState();
                _authenticatorListAdapter.NotifyItemInserted(_authenticatorSource.GetPosition(auth.Secret));
                await NotifyWearAppOfChange();
            }
            catch
            {
                Toast.MakeText(this, Resource.String.qrCodeFormatError, ToastLength.Short).Show();
            }
        }

        private void OpenQRCodeScanner(object sender, EventArgs e)
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            else
                ScanQRCode();
        }

        /*
         *  Add Dialog
         */

        private void OpenAddDialog(object sender, EventArgs e)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);
            _addDialog = new AddAuthenticatorDialog();
            _addDialog.Add += OnAddDialogSubmit;
            _addDialog.Show(transaction, "add_dialog");
        }

        private async void OnAddDialogSubmit(object sender, AddAuthenticatorDialog.AddAuthenticatorEventArgs e)
        {
            var hasError = false;

            if(e.Issuer.Trim() == "")
            {
                _addDialog.IssuerError = GetString(Resource.String.noIssuer);
                hasError = true;
            }

            var secret = Authenticator.CleanSecret(e.Secret);

            if(secret == "")
            {
                _addDialog.SecretError = GetString(Resource.String.noSecret);
                hasError = true;
            }
            else if(!Authenticator.IsValidSecret(secret))
            {
                _addDialog.SecretError = GetString(Resource.String.secretInvalid);
                hasError = true;
            }

            if(e.Digits < 6 || e.Digits > 10)
            {
                _addDialog.DigitsError = GetString(Resource.String.digitsInvalid);
                hasError = true;
            }

            if(e.Period <= 0)
            {
                _addDialog.PeriodError = GetString(Resource.String.periodToShort);
                hasError = true;
            }

            if(hasError)
                return;

            var issuer = e.Issuer.Trim().Truncate(32);
            var username = e.Username.Trim().Truncate(32);

            var auth = new Authenticator {
                Issuer = issuer,
                Username = username,
                Type = e.Type,
                Icon = Icon.FindServiceKeyByName(issuer),
                Algorithm = e.Algorithm,
                Secret = secret,
                Digits = e.Digits,
                Period = e.Period
            };

            if(_authenticatorSource.IsDuplicate(auth))
            {
                _addDialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }

            await _connection.InsertAsync(auth);
            await _authenticatorSource.Update();

            CheckEmptyState();
            _authenticatorListAdapter.NotifyItemInserted(_authenticatorSource.GetPosition(auth.Secret));
            await NotifyWearAppOfChange();

            _addDialog.Dismiss();
        }

        /*
         *  Rename Dialog
         */

        private void OpenRenameDialog(int position)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);
            _renameDialog = new RenameAuthenticatorDialog(auth.Issuer, auth.Username, position);
            _renameDialog.Rename += OnRenameDialogSubmit;
            _renameDialog.Show(transaction, "rename_dialog");
        }

        private async void OnRenameDialogSubmit(object sender, RenameAuthenticatorDialog.RenameEventArgs e)
        {
            if(e.Issuer.Trim() == "")
            {
                _renameDialog.IssuerError = GetString(Resource.String.noIssuer);
                return;
            }

            await _authenticatorSource.Rename(e.ItemPosition, e.Issuer, e.Username);
            _authenticatorListAdapter.NotifyItemChanged(e.ItemPosition);
            await NotifyWearAppOfChange();
            _renameDialog?.Dismiss();
        }

        /*
         *  Icon Dialog
         */

        private void OpenIconDialog(int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("icon_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);
            _iconDialog = new IconDialog(position, IsDark);
            _iconDialog.IconSelected += OnIconDialogIconSelected;
            _iconDialog.Show(transaction, "icon_dialog");
        }

        private async void OnIconDialogIconSelected(object sender, IconDialog.IconSelectedEventArgs e)
        {
            var auth = _authenticatorSource.Authenticators.ElementAtOrDefault(e.ItemPosition);

            if(auth == null)
                return;

            auth.Icon = e.Icon;

            await _connection.UpdateAsync(auth);
            _authenticatorListAdapter.NotifyItemChanged(e.ItemPosition);
            await NotifyWearAppOfChange();

            _iconDialog?.Dismiss();
        }

        /*
         *  Categories Dialog
         */

        private void OpenCategoriesDialog(int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("categories_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);

            _categoriesDialog =
                new ChooseCategoriesDialog(_categorySource, position, _authenticatorSource.GetCategories(position));
            _categoriesDialog.CategoryClick += OnCategoriesDialogCategoryClick;
            _categoriesDialog.Close += OnCategoriesDialogClose;

            _categoriesDialog.Show(transaction, "categories_dialog");
        }

        private void OnCategoriesDialogClose(object sender, EventArgs e)
        {
            if(_authenticatorSource.CategoryId != null)
            {
                _authenticatorSource.UpdateView();
                _authenticatorListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            }

            _categoriesDialog.Dismiss();
        }

        private void OnCategoriesDialogCategoryClick(object sender, ChooseCategoriesDialog.CategoryClickedEventArgs e)
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
    }
}