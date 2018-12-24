using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using OtpSharp;
using ProAuth.Data;
using ProAuth.Dialogs;
using ProAuth.Utilities;
using ProAuth.Utilities.AuthenticatorList;
using ProAuth.Utilities.CategoryList;
using SQLite;
using ZXing;
using ZXing.Mobile;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using SearchView = Android.Support.V7.Widget.SearchView;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace ProAuth.Activities
{
    [Activity(Label = "@string/displayName", Theme = "@style/LightTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    // ReSharper disable once UnusedMember.Global
    public class ActivityMain : AppCompatActivity
    {
        // State
        private Timer _authTimer;
        private DateTime _pauseTime;
        private ISharedPreferences _sharedPrefs;

        // Views
        private RecyclerView _authList;
        private LinearLayout _emptyState;
        private FloatingActionButton _addButton;
        private SearchView _searchView;
        private DrawerLayout _drawerLayout;
        private CustomActionBarDrawerToggle _actionBarDrawerToggle;
        private NavigationView _navigationView;
        private ProgressBar _progressBar;
        private ISubMenu _categoriesMenu;

        // Data
        private AuthAdapter _authAdapter;
        private AuthSource _authSource;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private MobileBarcodeScanner _barcodeScanner;
        private KeyguardManager _keyguardManager;

        // Alert Dialogs
        private DialogRenameAuthenticator _renameDialog;
        private DialogAddAuthenticator _addDialog;
        private DialogIcon _iconDialog;
        private DialogChooseCategories _categoriesDialog;

        public ActivityMain()
        {
            _pauseTime = DateTime.MinValue;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ThemeHelper.Update(this);
            SetContentView(Resource.Layout.activityMain);

            // Actionbar
            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.activityMain_progressBar);

            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.categoryAll);
            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            // Navigation Drawer
            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.activityMain_drawerLayout);
            _navigationView = FindViewById<NavigationView>(Resource.Id.activityMain_navView);
            _navigationView.NavigationItemSelected += DrawerItemSelected;

            _actionBarDrawerToggle = new CustomActionBarDrawerToggle(this, _drawerLayout, toolbar, Resource.String.appName, Resource.String.appName);
            _drawerLayout.AddDrawerListener(_actionBarDrawerToggle);

            // Buttons
            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _addButton.Click += AddButtonClick;

            // Barcode scanner
            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            View overlay = LayoutInflater.Inflate(Resource.Layout.qrCode, null);
            _barcodeScanner.CustomOverlay = overlay;
            _barcodeScanner.UseCustomOverlay = true;

            // Misc
            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);

            // Recyclerview
            _authList = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);
            _emptyState = FindViewById<LinearLayout>(Resource.Id.activityMain_emptyState);
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);
            _actionBarDrawerToggle.SyncState();
        }

        protected override void OnResume()
        {
            base.OnResume();

            _sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool firstLaunch = _sharedPrefs.GetBoolean("firstLaunch", true);

            if(firstLaunch)
            {
                StartActivity(typeof(ActivityIntro));
                return;
            }

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1 && PerformLogin())
            {
                return;
            }

            // Just launched
            if(_connection == null)
            {
                Init();
            }
            else
            {
                UpdateAuthenticators();
                UpdateCategories();

                // Currently visible category has been deleted
                if(_authSource.CategoryId != null &&
                   _categorySource.Categories.FirstOrDefault(c => c.Id == _authSource.CategoryId) == null)
                {
                    SwitchCategory(-1);
                }
            }

            _authTimer?.Start();
        }

        private async void Init()
        {
            _connection = await Database.Connect();
            InitCategories();
            InitAuthenticators();

            UpdateCategories(false);
            UpdateAuthenticators(false);

            CreateTimer();
        }

        private void InitAuthenticators()
        {
            _authSource = new AuthSource(_connection);
            _authAdapter = new AuthAdapter(_authSource);

            _authAdapter.ItemClick += ItemClick;
            _authAdapter.ItemOptionsClick += ItemOptionsClick;
            _authAdapter.HasStableIds = true;

            _authList.SetAdapter(_authAdapter);
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(20);

            LayoutAnimationController animation = 
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fall_down);
            _authList.LayoutAnimation = animation;

            bool useGrid = IsTablet();
            CustomGridLayoutManager layout = new CustomGridLayoutManager(this, useGrid ? 2 : 1);
            _authList.SetLayoutManager(layout);

            CustomTouchHelperCallback callback = new CustomTouchHelperCallback(_authAdapter, useGrid);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);
        }

        private async void UpdateAuthenticators(bool updateSource = true)
        {
            _progressBar.Visibility = ViewStates.Visible;
            await _authSource.UpdateTask;

            if(updateSource)
                await _authSource.UpdateSource();

            _authAdapter.NotifyDataSetChanged();
            CheckEmptyState();
            _authList.ScheduleLayoutAnimation();

            AlphaAnimation animation = new AlphaAnimation(1.0f, 0.0f)
            {
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

        private async void UpdateCategories(bool updateSource = true)
        {
            await _categorySource.UpdateTask;

            if(updateSource)
            {
                await _categorySource.Update();
            }

            _categoriesMenu.Clear();
            
            IMenuItem allItem = _categoriesMenu.Add(Menu.None, -1, Menu.None, Resource.String.categoryAll);
            allItem.SetChecked(true);

            for(int i = 0; i < _categorySource.Count(); ++i)
            {
                _categoriesMenu.Add(0, i, i, _categorySource.Categories[i].Name);
            }
        }

        private void CheckEmptyState()
        {
            if(_authSource.Count() == 0)
            {
                _emptyState.Visibility = ViewStates.Visible;
                _authList.Visibility = ViewStates.Gone;

                AlphaAnimation animation = new AlphaAnimation(0.0f, 1.0f) {
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

            IMenuItem searchItem = menu.FindItem(Resource.Id.actionSearch);
            _searchView = (SearchView) searchItem.ActionView;
            _searchView.QueryHint = GetString(Resource.String.search);

            _searchView.QueryTextChange += (sender, e) =>
            {
                _authSource.SetSearch(e.NewText);
                _authAdapter.NotifyDataSetChanged();
            };

            _searchView.Close += (sender, e) =>
            {
                _authSource.UpdateView();
            };

            return base.OnCreateOptionsMenu(menu);
        }

        private void DrawerItemSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            switch(e.MenuItem.ItemId)
            {
                case Resource.Id.drawerSettings:
                    _actionBarDrawerToggle.IdleAction = () =>
                    {
                        StartActivity(typeof(ActivitySettings));
                    };
                    break;

                case Resource.Id.drawerEditCategories:
                    _actionBarDrawerToggle.IdleAction = () =>
                    {
                        StartActivity(typeof(ActivityEditCategories));
                    };
                    break;

                case Resource.Id.drawerRestore:
                    _actionBarDrawerToggle.IdleAction = () =>
                    {
                        StartActivity(typeof(ActivityRestore));
                    };
                    break;

                case Resource.Id.drawerBackup:
                    _actionBarDrawerToggle.IdleAction = () =>
                    {
                        StartActivity(typeof(ActivityBackup));
                    };
                    break;

                default:
                    _actionBarDrawerToggle.IdleAction = () =>
                    {
                        int position = e.MenuItem.ItemId;
                        SwitchCategory(position);
                    };
                    break;
            }

            _drawerLayout.CloseDrawers();
        }

        private void SwitchCategory(int position)
        {
            if(position == -1)
            {
                _authSource.SetCategory(null);
                SupportActionBar.Title = GetString(Resource.String.categoryAll);
            }
            else
            {
                Category category = _categorySource.Categories[position];
                _authSource.SetCategory(category.Id);
                SupportActionBar.Title = category.Name;
            }

            _categoriesMenu.FindItem(position).SetChecked(true);
            UpdateAuthenticators(false);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(_actionBarDrawerToggle.OnOptionsItemSelected(item))
            {
                return true;
            }

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
            bool authRequired = _sharedPrefs.GetBoolean("pref_appLock", false);

            bool isDeviceSecure = Build.VERSION.SdkInt <= Build.VERSION_CODES.Lollipop
                ? _keyguardManager.IsKeyguardSecure 
                : _keyguardManager.IsDeviceSecure;

            if(authRequired && isDeviceSecure)
            {
                StartActivity(typeof(ActivityLogin));
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
            {
                return;
            }

            int start = 0;
            int stop = _authSource.Authenticators.Count;

            for(int i = start; i < stop; ++i)
            {
                IAuthenticatorInfo auth = _authSource.Authenticators[i];
                int position = i; // Closure modification

                if(auth.Type == OtpType.Totp)
                {
                    if(auth.TimeRenew > DateTime.Now)
                    {
                        int secondsRemaining = (auth.TimeRenew - DateTime.Now).Seconds;
                        int progress = 100 * secondsRemaining / auth.Period;

                        RunOnUiThread(() =>
                        {
                            _authAdapter.NotifyItemChanged(position, progress);
                        });
                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {
                            _authAdapter.NotifyItemChanged(position);
                        });
                    }
                }
                else if(auth.Type == OtpType.Hotp && auth.TimeRenew < DateTime.Now)
                {
                    RunOnUiThread(() =>
                    {
                        _authAdapter.NotifyItemChanged(position, true);
                    }); 
                }
            }
        }

        private void ItemClick(object sender, int position)
        {
            ClipboardManager clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            Authenticator auth = _authSource.Get(position);
            ClipData clip = ClipData.NewPlainText("code", auth.Code);
            clipboard.PrimaryClip = clip;

            Toast.MakeText(this, Resource.String.copiedToClipboard, ToastLength.Short).Show();
        }

        private void ItemOptionsClick(object sender, int position)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
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

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        public bool IsTablet()
        {
            Display display = WindowManager.DefaultDisplay;
            DisplayMetrics displayMetrics = new DisplayMetrics();
            display.GetMetrics(displayMetrics);

            var wInches = displayMetrics.WidthPixels / (double)displayMetrics.DensityDpi;
            var hInches = displayMetrics.HeightPixels / (double)displayMetrics.DensityDpi;

            double screenDiagonal = Math.Sqrt(Math.Pow(wInches, 2) + Math.Pow(hInches, 2));
            return (screenDiagonal >= 7.0);
        }

        private void OpenDeleteDialog(int position)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirmAuthenticatorDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, (sender, args) =>
            {
                _authSource.Delete(position);
                _authAdapter.NotifyItemRemoved(position);
                CheckEmptyState();
            });
            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });
            builder.SetCancelable(true);

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void AddButtonClick(object sender, EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, _addButton);
            menu.Inflate(Resource.Menu.add);
            menu.MenuItemClick += AddMenuItemClick;
            menu.Show();
        }

        private void AddMenuItemClick(object sender, PopupMenu.MenuItemClickEventArgs e)
        {
            switch(e.Item.ItemId)
            {
                case Resource.Id.actionScan:
                    ScanQRCode();
                    break;

                case Resource.Id.actionEnterKey:
                    OpenAddDialog();
                    break;
            }
        }

        private async void ScanQRCode()
        {
            MobileBarcodeScanningOptions options = new MobileBarcodeScanningOptions {
                PossibleFormats = new List<BarcodeFormat> {
                    BarcodeFormat.QR_CODE
                }
            };

            ZXing.Result result = await _barcodeScanner.Scan(options);

            if(result == null)
            {
                return;
            }

            try
            {
                Authenticator auth = Authenticator.FromKeyUri(result.Text);

                if(_authSource.IsDuplicate(auth))
                {
                    Toast.MakeText(this, Resource.String.duplicateAuthenticator, ToastLength.Short).Show();
                    return;
                }

                await _connection.InsertAsync(auth);
                await _authSource.UpdateSource();

                CheckEmptyState();
                _authAdapter.NotifyDataSetChanged();
            }
            catch
            {
                Toast.MakeText(this, Resource.String.qrCodeFormatError, ToastLength.Short).Show();
            }
        }

        /*
         *  Add Dialog
         */
        private void OpenAddDialog()
        {
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            _addDialog = new DialogAddAuthenticator(AddDialogPositive, AddDialogNegative);
            _addDialog.Show(transaction, "add_dialog");
        }

        private async void AddDialogPositive(object sender, EventArgs e)
        {
            bool error = false;

            if(_addDialog.Issuer.Trim() == "")
            {
                _addDialog.IssuerError = GetString(Resource.String.noIssuer);
                error = true;
            }

            if(_addDialog.Secret.Trim() == "")
            {
                _addDialog.SecretError = GetString(Resource.String.noSecret);
                error = true;
            }

            string secret = _addDialog.Secret.Trim().ToUpper();
            const string base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            const char base32Padding = '=';

            if(secret.Length < 16)
            {
                _addDialog.SecretError = GetString(Resource.String.secretTooShort);
                error = true;
            }

            if(!secret.ToCharArray().All(x => base32Alphabet.IndexOf(x) >= 0 || x == base32Padding))
            {
                _addDialog.SecretError = GetString(Resource.String.secretInvalidChars);
                error = true;
            }

            if(_addDialog.Digits < 6)
            {
                _addDialog.DigitsError = GetString(Resource.String.digitsToSmall);
                error = true;
            }

            if(_addDialog.Period < 10)
            {
                _addDialog.PeriodError = GetString(Resource.String.periodToShort);
                error = true;
            }

            if(error)
            {
                return;
            }

            string issuer = _addDialog.Issuer.Trim().Truncate(32);
            string username = _addDialog.Username.Trim().Truncate(32);

            OtpHashMode algorithm = OtpHashMode.Sha1;
            switch(_addDialog.Algorithm)
            {
                case 1:
                    algorithm = OtpHashMode.Sha256;
                    break;
                case 2:
                    algorithm = OtpHashMode.Sha512;
                    break;
            }

            OtpType type = (_addDialog.Type == 0) ? OtpType.Totp : OtpType.Hotp;

            string code = "";
            for(int i = 0; i < _addDialog.Digits; code += "-", i++);

            Authenticator auth = new Authenticator {
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
                Toast.MakeText(this, Resource.String.duplicateAuthenticator, ToastLength.Short).Show();
                return;
            }

            await _connection.InsertAsync(auth);
            await _authSource.UpdateSource();

            CheckEmptyState();
            _authAdapter.NotifyDataSetChanged();

            _addDialog.Dismiss();
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
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            Authenticator auth = _authSource.Get(position);
            _renameDialog = new DialogRenameAuthenticator(RenameDialogPositive, RenameDialogNegative, auth, position);
            _renameDialog.Show(transaction, "rename_dialog");
        }

        private void RenameDialogPositive(object sender, EventArgs e)
        {
            if(_renameDialog.Issuer.Trim() == "")
            {
                _renameDialog.IssuerError = GetString(Resource.String.noIssuer);
                return;
            }

            string issuer = _renameDialog.Issuer;
            string username = _renameDialog.Username;

            _authSource.Rename(_renameDialog.Position, issuer, username);
            _authAdapter.NotifyItemChanged(_renameDialog.Position);
            _renameDialog?.Dismiss();
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
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("icon_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            _iconDialog = new DialogIcon(IconDialogIconClick, IconDialogNegative, position);
            _iconDialog.Show(transaction, "icon_dialog");
        }

        private async void IconDialogIconClick(object sender, EventArgs e)
        {
            Authenticator auth = _authSource.Get(_iconDialog.Position);
            auth.Icon = _iconDialog.IconKey;

            await _connection.UpdateAsync(auth);
            _authAdapter.NotifyItemChanged(_iconDialog.Position);

            _iconDialog?.Dismiss();
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
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("categories_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);

            _categoriesDialog = 
                new DialogChooseCategories(_categorySource, CategoriesDialogOnClose, CategoriesDialogOnClick, position, _authSource.GetCategories(position));
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
            string categoryId = _categorySource.Categories[categoryPosition].Id;
            int authPosition = _categoriesDialog.AuthPosition;

            if(isChecked)
            {
                _authSource.AddToCategory(authPosition, categoryId);
            }
            else
            {
                _authSource.RemoveFromCategory(authPosition, categoryId);
            }
        }
    }
}

