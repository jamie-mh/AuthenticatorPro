using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using System.Timers;
using Android.Content;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using ZXing;
using ZXing.Mobile;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using System;
using System.Linq;
using SearchView = Android.Support.V7.Widget.SearchView;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Support.V7.Widget.Helper;
using OtpSharp;
using ProAuth.Data;
using ProAuth.Utilities;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace ProAuth
{
    [Activity(Label = "@string/appName", Theme = "@style/LightTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    // ReSharper disable once UnusedMember.Global
    public class ActivityMain : AppCompatActivity
    {
        // Results
        private const int RequestConfirmDeviceCredentials = 0;

        // State
        private Timer _authTimer;
        private DateTime _pauseTime;

        // Views
        private RecyclerView _authList;
        private AuthAdapter _authAdapter;
        private AuthSource _authSource;

        private LinearLayout _emptyState;
        private FloatingActionButton _addButton;
        private SearchView _searchView;

        // Data
        private Database _database;
        private MobileBarcodeScanner _barcodeScanner;
        private KeyguardManager _keyguardManager;

        // Alert Dialogs
        private DialogRename _renameDialog;
        private DialogAdd _addDialog;
        private DialogIcon _iconDialog;

        public ActivityMain()
        {
            _pauseTime = DateTime.MinValue;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityMain);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.appName);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _addButton.Click += AddButtonClick;

            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            View overlay = LayoutInflater.Inflate(Resource.Layout.qrCode, null);
            _barcodeScanner.CustomOverlay = overlay;
            _barcodeScanner.UseCustomOverlay = true;

            _database = new Database();

            CreateAuthenticatorList();
            UpdateEmptyState();
            CreateTimer();

            _keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);
        }

        private void CreateAuthenticatorList()
        {
            _authList = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);
            _emptyState = FindViewById<LinearLayout>(Resource.Id.activityMain_emptyState);

            _authSource = new AuthSource(_database.Connection);
            _authAdapter = new AuthAdapter(_authSource);
            _authAdapter.ItemClick += ItemClick;
            _authAdapter.ItemOptionsClick += ItemOptionsClick;

            _authList.SetAdapter(_authAdapter);
            _authList.HasFixedSize = true;
            _authList.SetItemViewCacheSize(20);
            _authList.DrawingCacheEnabled = true;
            _authList.DrawingCacheQuality = DrawingCacheQuality.High;
            _authList.SetLayoutManager(new LinearLayoutManager(this));

            AuthTouchHelperCallback callback = new AuthTouchHelperCallback(_authAdapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_authList);

            Tick(null, null);
        }

        private void UpdateEmptyState()
        {
            if(_authSource.Count() == 0)
            {
                _emptyState.Visibility = ViewStates.Visible;
                _authList.Visibility = ViewStates.Gone;
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
            base.OnDestroy();
            _database?.Connection.Close();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Android.App.Result resultCode, Intent data)
        {
            if(requestCode == RequestConfirmDeviceCredentials)
            {
                switch(resultCode)
                {
                    case Android.App.Result.Canceled:
                        Finish();
                        break;

                    case Android.App.Result.Ok:
                        break;
                }
            }
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

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Resource.Id.actionSettings:
                    StartActivity(typeof(ActivitySettings));
                    break;

                case Resource.Id.actionImport:
                    StartActivity(typeof(ActivityImport));
                    break;

                case Resource.Id.actionExport:
                    StartActivity(typeof(ActivityExport));
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _authTimer.Stop();
            _pauseTime = DateTime.Now;
        }

        private void Login()
        {
            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool authRequired = sharedPrefs.GetBoolean("pref_requireAuthentication", false);

            if(authRequired && _keyguardManager.IsDeviceSecure)
            {
                Intent loginIntent = _keyguardManager.CreateConfirmDeviceCredentialIntent(GetString(Resource.String.login), GetString(Resource.String.loginMessage));

                if(loginIntent != null)
                {
                    StartActivityForResult(loginIntent, RequestConfirmDeviceCredentials);
                }
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            _authTimer.Start();

            if((DateTime.Now - _pauseTime).TotalMinutes >= 1)
            {
                Login();
            }

            _authSource.Update();
            _authAdapter.NotifyDataSetChanged();
            UpdateEmptyState();
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
            int start = 0;
            int stop = _authSource.Authenticators.Count;

            for(int i = start; i < stop; ++i)
            {
                Authenticator auth = _authSource.Authenticators[i];

                if(auth.Type == OtpType.Hotp)
                {
                    continue;
                }

                int position = i; // Closure modification

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
                        _authAdapter.NotifyItemChanged(position, null);
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
                        OpenDeleteDialog(position);
                        break;
                }
            });

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void OpenDeleteDialog(int position)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirmDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetPositiveButton(Resource.String.delete, (sender, args) =>
            {
                _authSource.Delete(position);
                _authAdapter.NotifyItemRemoved(position);
                UpdateEmptyState();
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

                _database.Connection.Insert(auth);
                _authSource.Update();
                UpdateEmptyState();
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
            _addDialog = new DialogAdd(AddDialogPositive, AddDialogNegative);
            _addDialog.Show(transaction, "add_dialog");
        }

        private void AddDialogPositive(object sender, EventArgs e)
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
                Icon = Icons.FindKeyByName(issuer),
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

            _database.Connection.Insert(auth);
            _authSource.Update();
            UpdateEmptyState();
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
            _renameDialog = new DialogRename(RenameDialogPositive, RenameDialogNegative, auth, position);
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

        private void IconDialogIconClick(object sender, EventArgs e)
        {
            Authenticator auth = _authSource.Get(_iconDialog.Position);
            auth.Icon = _iconDialog.IconKey;

            _database.Connection.Update(auth);
            _authAdapter.NotifyItemChanged(_iconDialog.Position);

            _iconDialog?.Dismiss();
        }

        private void IconDialogNegative(object sender, EventArgs e)
        {
            _iconDialog.Dismiss();
        }
    }
}

