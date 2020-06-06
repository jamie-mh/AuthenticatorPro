using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
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
using AuthenticatorPro.Dialogs;
using AuthenticatorPro.AuthenticatorList;
using AuthenticatorPro.CategoryList;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Navigation;
using SQLite;
using ZXing;
using ZXing.Mobile;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using SearchView = AndroidX.AppCompat.Widget.SearchView;
using SQLiteException = SQLite.SQLiteException;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using AuthenticatorPro.Fragments;
using AuthenticatorPro.Shared;
using Newtonsoft.Json;
using OtpNet;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "@string/displayName", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    internal class MainActivity : LightDarkActivity
    {
        private const string WearRefreshCapability = "refresh";

        private const int PermissionCameraCode = 0;

        private IdleActionBarDrawerToggle _actionBarDrawerToggle;
        private FloatingActionButton _addButton;
        private AddAuthenticatorDialog _addDialog;

        private AuthAdapter _authAdapter;
        private RecyclerView _authList;
        private AuthSource _authSource;

        // State
        private Timer _authTimer;
        private MobileBarcodeScanner _barcodeScanner;
        private ChooseCategoriesDialog _categoriesDialog;
        private ISubMenu _categoriesMenu;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private DrawerLayout _drawerLayout;
        private LinearLayout _emptyState;
        private IconDialog _iconDialog;
        private KeyguardManager _keyguardManager;
        private NavigationView _navigationView;
        private DateTime _pauseTime;
        private ProgressBar _progressBar;

        private bool _isChildActivityOpen;

        // Alert Dialogs
        private RenameAuthenticatorDialog _renameDialog;
        private SearchView _searchView;
        private ISharedPreferences _sharedPrefs;

        public MainActivity()
        {
            _pauseTime = DateTime.MinValue;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            SetContentView(Resource.Layout.activityMain);

            // Actionbar
            var toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.activityMain_progressBar);

            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // Navigation Drawer
            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.activityMain_drawerLayout);
            _navigationView = FindViewById<NavigationView>(Resource.Id.activityMain_navView);
            _navigationView.NavigationItemSelected += DrawerItemSelected;

            _actionBarDrawerToggle = new IdleActionBarDrawerToggle(this, _drawerLayout, toolbar,
                Resource.String.appName, Resource.String.appName);
            _drawerLayout.AddDrawerListener(_actionBarDrawerToggle);

            // Buttons
            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _addButton.Click += AddButtonClick;

            // Barcode scanner
            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            // Misc
            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            // Recyclerview
            _authList = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);
            _emptyState = FindViewById<LinearLayout>(Resource.Id.activityMain_emptyState);

            _isChildActivityOpen = false;
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            _actionBarDrawerToggle.SyncState();
        }

        protected override async void OnResume()
        {
            base.OnResume();

            _sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var firstLaunch = _sharedPrefs.GetBoolean("firstLaunch", true);

            if(firstLaunch)
            {
                StartChildActivity(typeof(IntroActivity));
                return;
            }

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1 && PerformLogin()) return;

            // Just launched
            if(_connection == null)
            {
                await Init();
            }
            else if(_isChildActivityOpen)
            {
                var isCompact = _sharedPrefs.GetBoolean("pref_compactMode", false);
                if(isCompact != _authAdapter.IsCompact)
                {
                    Recreate();
                    return;
                }

                await UpdateAuthenticators();
                await UpdateCategories();

                // Currently visible category has been deleted
                if(_authSource.CategoryId != null &&
                   _categorySource.Categories.FirstOrDefault(c => c.Id == _authSource.CategoryId) == null)
                    await SwitchCategory(-1);
            }
            else
                _authList.Visibility = ViewStates.Visible;

            _isChildActivityOpen = false;
            Tick(null, null);
            _authTimer?.Start();
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

                InitCategories();
                InitAuthenticators();

                await UpdateCategories(false);
                await UpdateAuthenticators(false);

                CreateTimer();
            }
            catch(SQLiteException)
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
        }

        private void InitAuthenticators()
        {
            _authSource = new AuthSource(_connection);

            var isCompact = _sharedPrefs.GetBoolean("pref_compactMode", false);
            _authAdapter = new AuthAdapter(_authSource, IsDark, isCompact);

            _authAdapter.ItemClick += ItemClick;
            _authAdapter.ItemOptionsClick += ItemOptionsClick;
            _authAdapter.ItemMoved += async (sender, i) =>
            {
                await NotifyWearChanged();
            };

            _authAdapter.SetHasStableIds(true);

            _authList.SetAdapter(_authAdapter);
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(20);

            var animation =
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            var useGrid = IsTablet();
            var layout = new AuthListGridLayoutManager(this, useGrid ? 2 : 1);
            _authList.SetLayoutManager(layout);

            var callback = new AuthListTouchHelperCallback(_authAdapter, useGrid);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);
        }

        private async Task UpdateAuthenticators(bool updateSource = true)
        {
            _progressBar.Visibility = ViewStates.Visible;
            await _authSource.UpdateTask;

            if(updateSource)
                await _authSource.UpdateSource();

            _authAdapter.NotifyDataSetChanged();
            CheckEmptyState();
            _authList.ScheduleLayoutAnimation();

            var animation = new AlphaAnimation(1.0f, 0.0f) {
                Duration = 200
            };
            animation.AnimationEnd += (sender, e) => { _progressBar.Visibility = ViewStates.Invisible; };
            _progressBar.StartAnimation(animation);
        }

        private void InitCategories()
        {
            _categoriesMenu =
                _navigationView.Menu.AddSubMenu(Menu.None, Menu.None, Menu.None, Resource.String.categories);
            _categoriesMenu.SetGroupCheckable(0, true, true);

            _categorySource = new CategorySource(_connection);
        }

        private async Task UpdateCategories(bool updateSource = true)
        {
            await _categorySource.UpdateTask;

            if(updateSource) await _categorySource.Update();

            _categoriesMenu.Clear();

            var allItem = _categoriesMenu.Add(Menu.None, -1, Menu.None, Resource.String.categoryAll);
            allItem.SetChecked(true);

            for(var i = 0; i < _categorySource.Count(); ++i)
                _categoriesMenu.Add(0, i, i, _categorySource.Categories[i].Name);
        }

        private void CheckEmptyState()
        {
            if(_authSource.Count() == 0)
            {
                _emptyState.Visibility = ViewStates.Visible;
                _authList.Visibility = ViewStates.Gone;

                var animation = new AlphaAnimation(0.0f, 1.0f) {
                    Duration = 500
                };
                _emptyState.Animation = animation;
            }
            else
            {
                _emptyState.Visibility = ViewStates.Gone;
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

        protected override void OnDestroy()
        {
            _connection?.CloseAsync();
            base.OnDestroy();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);

            var searchItem = menu.FindItem(Resource.Id.actionSearch);
            _searchView = (SearchView) searchItem.ActionView;
            _searchView.QueryHint = GetString(Resource.String.search);

            _searchView.QueryTextChange += (sender, e) =>
            {
                _authSource.SetSearch(e.NewText);
                _authAdapter.NotifyDataSetChanged();
            };

            _searchView.Close += (sender, e) => { _authSource.UpdateView(); };

            return base.OnCreateOptionsMenu(menu);
        }

        private void DrawerItemSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            switch(e.MenuItem.ItemId)
            {
                case Resource.Id.drawerSettings:
                    _actionBarDrawerToggle.IdleAction = () => { StartChildActivity(typeof(SettingsActivity)); };
                    break;

                case Resource.Id.drawerEditCategories:
                    _actionBarDrawerToggle.IdleAction = () => { StartChildActivity(typeof(EditCategoriesActivity)); };
                    break;

                case Resource.Id.drawerRestore:
                    _actionBarDrawerToggle.IdleAction = () => { StartChildActivity(typeof(RestoreActivity)); };
                    break;

                case Resource.Id.drawerBackup:
                    _actionBarDrawerToggle.IdleAction = () => { StartChildActivity(typeof(BackupActivity)); };
                    break;

                default:
                    _actionBarDrawerToggle.IdleAction = async () =>
                    {
                        var position = e.MenuItem.ItemId;
                        await SwitchCategory(position);
                    };
                    break;
            }

            _drawerLayout.CloseDrawers();
        }

        private async Task SwitchCategory(int position)
        {
            if(position == -1)
            {
                _authSource.SetCategory(null);
                SupportActionBar.Title = GetString(Resource.String.categoryAll);
            }
            else
            {
                var category = _categorySource.Categories[position];
                _authSource.SetCategory(category.Id);
                SupportActionBar.Title = category.Name;
            }

            await UpdateAuthenticators(false);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(_actionBarDrawerToggle.OnOptionsItemSelected(item))
                return true;

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnPause()
        {
            base.OnPause();

            _authTimer?.Stop();
            _pauseTime = DateTime.Now;
            _authList.Visibility = ViewStates.Gone;
        }

        private bool PerformLogin()
        {
            var authRequired = _sharedPrefs.GetBoolean("pref_appLock", false);

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
            {
                base.OnBackPressed();
            }
            else
            {
                _searchView.OnActionViewCollapsed();
                _searchView.Iconified = true;
            }
        }

        private void Tick(object sender, ElapsedEventArgs e)
        {
            if(_authSource == null)
                return;

            var stop = _authSource.Authenticators.Count;

            for(var i = 0; i < stop; ++i)
            {
                var auth = _authSource.Authenticators[i];
                var position = i; // Closure modification

                if(auth.Type == AuthenticatorType.Totp && auth.TimeRenew > DateTime.Now ||
                   auth.Type == AuthenticatorType.Hotp && auth.TimeRenew < DateTime.Now)
                    RunOnUiThread(() => { _authAdapter.NotifyItemChanged(position, true); });

                else if(auth.Type == AuthenticatorType.Totp)
                    RunOnUiThread(() => { _authAdapter.NotifyItemChanged(position); });
            }
        }

        private void ItemClick(object sender, int position)
        {
            if(position < 0)
                return;

            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var auth = _authSource.Get(position);
            var clip = ClipData.NewPlainText("code", auth.Code);
            clipboard.PrimaryClip = clip;

            Toast.MakeText(this, Resource.String.copiedToClipboard, ToastLength.Short).Show();
        }

        private void ItemOptionsClick(object sender, int position)
        {
            if(position < 0)
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
                await _authSource.Delete(position);
                _authAdapter.NotifyItemRemoved(position);
                CheckEmptyState();
                await NotifyWearChanged();
            });
            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            var fragment = new AddBottomSheetDialogFragment {
                ClickQrCode = OpenQRCodeScanner, ClickEnterKey = OpenAddDialog
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

            if(result == null) return;

            try
            {
                var auth = Authenticator.FromKeyUri(result.Text);

                if(_authSource.IsDuplicate(auth))
                {
                    Toast.MakeText(this, Resource.String.duplicateAuthenticator, ToastLength.Short).Show();
                    return;
                }

                await _connection.InsertAsync(auth);
                await _authSource.UpdateSource();

                CheckEmptyState();
                _authAdapter.NotifyItemInserted(_authSource.GetPosition(auth.Secret));
                await NotifyWearChanged();
            }
            catch
            {
                Toast.MakeText(this, Resource.String.qrCodeFormatError, ToastLength.Short).Show();
            }
        }

        private void OpenQRCodeScanner()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != Permission.Granted)
                ActivityCompat.RequestPermissions(this, new[] { Manifest.Permission.Camera }, PermissionCameraCode);
            else
                ScanQRCode();
        }

        /*
         *  Add Dialog
         */

        private void OpenAddDialog()
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _addDialog = new AddAuthenticatorDialog(AddDialogPositive, AddDialogNegative);
            _addDialog.Show(transaction, "add_dialog");
        }

        private async void AddDialogPositive(object sender, EventArgs e)
        {
            var error = false;

            if(_addDialog.Issuer.Trim() == "")
            {
                _addDialog.IssuerError = GetString(Resource.String.noIssuer);
                error = true;
            }

            var secret = Authenticator.CleanSecret(_addDialog.Secret);

            if(secret == "")
            {
                _addDialog.SecretError = GetString(Resource.String.noSecret);
                error = true;
            }
            else if(!Authenticator.IsValidSecret(secret))
            {
                _addDialog.SecretError = GetString(Resource.String.secretInvalid);
                error = true;
            }

            if(_addDialog.Digits < 6 || _addDialog.Digits > 10)
            {
                _addDialog.DigitsError = GetString(Resource.String.digitsInvalid);
                error = true;
            }

            if(_addDialog.Period <= 0)
            {
                _addDialog.PeriodError = GetString(Resource.String.periodToShort);
                error = true;
            }

            if(error) return;

            var issuer = _addDialog.Issuer.Trim().Truncate(32);
            var username = _addDialog.Username.Trim().Truncate(32);

            var algorithm = _addDialog.Algorithm switch {
                1 => OtpHashMode.Sha256,
                2 => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1
            };

            var type = _addDialog.Type == 0 ? AuthenticatorType.Totp : AuthenticatorType.Hotp;

            var code = "";
            for(var i = 0; i < _addDialog.Digits; code += "-", i++);

            var auth = new Authenticator {
                Issuer = issuer,
                Username = username,
                Type = type,
                Icon = Icons.FindServiceKeyByName(issuer),
                Algorithm = algorithm,
                Counter = 0,
                Secret = secret,
                Digits = _addDialog.Digits,
                Period = _addDialog.Period,
                Code = code
            };

            if(_authSource.IsDuplicate(auth))
            {
                _addDialog.SecretError = GetString(Resource.String.duplicateAuthenticator);
                return;
            }

            await _connection.InsertAsync(auth);
            await _authSource.UpdateSource();

            CheckEmptyState();
            _authAdapter.NotifyItemInserted(_authSource.GetPosition(auth.Secret));

            _addDialog.Dismiss();
            await NotifyWearChanged();
        }

        private void AddDialogNegative(object sender, EventArgs e)
        {
            _addDialog.Dismiss();
        }

        /*
         *  Rename Dialog
         */

        private void OpenRenameDialog(int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            var auth = _authSource.Get(position);
            _renameDialog = new RenameAuthenticatorDialog(RenameDialogPositive, RenameDialogNegative, auth, position);
            _renameDialog.Show(transaction, "rename_dialog");
        }

        private async void RenameDialogPositive(object sender, EventArgs e)
        {
            if(_renameDialog.Issuer.Trim() == "")
            {
                _renameDialog.IssuerError = GetString(Resource.String.noIssuer);
                return;
            }

            var issuer = _renameDialog.Issuer;
            var username = _renameDialog.Username;

            await _authSource.Rename(_renameDialog.Position, issuer, username);
            _authAdapter.NotifyItemChanged(_renameDialog.Position);
            _renameDialog?.Dismiss();
            await NotifyWearChanged();
        }

        private void RenameDialogNegative(object sender, EventArgs e)
        {
            _renameDialog.Dismiss();
        }

        /*
         *  Icon Dialog
         */

        private void OpenIconDialog(int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("icon_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _iconDialog = new IconDialog(IconDialogIconClick, IconDialogNegative, position, IsDark);
            _iconDialog.Show(transaction, "icon_dialog");
        }

        private async void IconDialogIconClick(object sender, EventArgs e)
        {
            var auth = _authSource.Get(_iconDialog.Position);
            auth.Icon = _iconDialog.IconKey;

            await _connection.UpdateAsync(auth);
            _authAdapter.NotifyItemChanged(_iconDialog.Position);

            _iconDialog?.Dismiss();
            await NotifyWearChanged();
        }

        private void IconDialogNegative(object sender, EventArgs e)
        {
            _iconDialog.Dismiss();
        }

        /*
         *  Categories Dialog
         */

        private void OpenCategoriesDialog(int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("categories_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);

            _categoriesDialog =
                new ChooseCategoriesDialog(_categorySource, CategoriesDialogOnClose, CategoriesDialogOnClick, position,
                    _authSource.GetCategories(position));
            _categoriesDialog.Show(transaction, "categories_dialog");
        }

        private void CategoriesDialogOnClose(object sender, EventArgs e)
        {
            if(_authSource.CategoryId != null)
            {
                _authSource.UpdateView();
                _authAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            }

            _categoriesDialog.Dismiss();
        }

        private void CategoriesDialogOnClick(bool isChecked, int categoryPosition)
        {
            var categoryId = _categorySource.Categories[categoryPosition].Id;
            var authPosition = _categoriesDialog.AuthPosition;

            if(isChecked)
                _authSource.AddToCategory(authPosition, categoryId);
            else
                _authSource.RemoveFromCategory(authPosition, categoryId);
        }

        /*
         *  Wear OS
         */

        private async Task NotifyWearChanged()
        {
            var nodes = (await WearableClass.GetCapabilityClient(this)
                .GetCapabilityAsync(WearRefreshCapability, CapabilityClient.FilterReachable)).Nodes;

            var client = WearableClass.GetMessageClient(this);

            foreach(var node in nodes)
                await client.SendMessageAsync(node.Id, WearRefreshCapability, new byte[] { });
        }
    }
}