using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using ProAuth.Utilities;
using System.Timers;
using Android.Content;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using ProAuth.Data;
using ZXing;
using ZXing.Mobile;
using PopupMenu = Android.Support.V7.Widget.PopupMenu;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using System;
using System.Linq;
using Android.Support.V4.View;
using SearchView = Android.Support.V7.Widget.SearchView;
using Android.Runtime;
using Android.Support.V7.Preferences;
using OtpSharp;
using Fragment = Android.Support.V4.App.Fragment;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace ProAuth
{
    [Activity(Label = "@string/appName", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    [MetaData("android.app.searchable", Resource = "@xml/searchable")]
    // ReSharper disable once UnusedMember.Global
    public class MainActivity : AppCompatActivity
    {
        private const int RequestConfirmDeviceCredentials = 0;

        private Timer _authTimer;
        private RecyclerView _authList;
        private FloatingActionButton _floatingActionButton;
        private AuthAdapter _authAdapter;
        private AuthSource _authSource;
        private Database _database;
        private MobileBarcodeScanner _barcodeScanner;
        private KeyguardManager _keyguardManager;

        // Alert Dialogs
        private RenameDialog _renameDialog;
        private AddDialog _addDialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityMain);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.appName);

            _keyguardManager = (KeyguardManager) GetSystemService(Context.KeyguardService);

            _floatingActionButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _floatingActionButton.Click += FloatingActionButtonClick;

            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            _database = new Database(this);
            _database.Prepare();

            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool authRequired = sharedPrefs.GetBoolean("pref_requireAuthentication", false);

            if(authRequired && _keyguardManager.IsDeviceSecure)
            {
                Intent loginIntent = _keyguardManager.CreateConfirmDeviceCredentialIntent(
                    GetString(Resource.String.login), GetString(Resource.String.loginMessage));

                if(loginIntent != null)
                {
                    StartActivityForResult(loginIntent, RequestConfirmDeviceCredentials);
                }
            }

            PrepareAuthenticatorList();

            _authTimer = new Timer()
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _authTimer.Elapsed += AuthTick;
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
            SearchView searchView = (SearchView) searchItem.ActionView;
            searchView.QueryHint = GetString(Resource.String.search);

            searchView.QueryTextChange += (sender, e) =>
            {
                _authSource.Search = e.NewText;
                _authSource.ClearCache();
                _authAdapter.NotifyDataSetChanged();
            };

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Resource.Id.actionSort:
                    ShowSortDialog();
                    break;

                case Resource.Id.actionSettings:
                    StartActivity(typeof(SettingsActivity));
                    break;

                case Resource.Id.actionImport:
                    StartActivity(typeof(ImportActivity));
                    break;

                case Resource.Id.actionExport:
                    StartActivity(typeof(ExportActivity));
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _authTimer.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _authTimer.Start();
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            _barcodeScanner.Cancel();
        }

        private void PrepareAuthenticatorList()
        {
            _authList = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);

            _authSource = new AuthSource(_database.Connection);
            _authAdapter = new AuthAdapter(_authSource);
            _authAdapter.ItemClick += AuthClick;
            _authAdapter.ItemOptionsClick += AuthOptionsClick;

            _authList.SetAdapter(_authAdapter);
            _authList.SetLayoutManager(new LinearLayoutManager(this));
        }

        private void AuthTick(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _authAdapter.NotifyDataSetChanged();
            });
        }

        private void AuthClick(object sender, int e)
        {
            ClipboardManager clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            Authenticator auth = _authSource.GetNth(e);
            ClipData clip = ClipData.NewPlainText("code", auth.Code);
            clipboard.PrimaryClip = clip;

            Toast.MakeText(this, Resource.String.copiedToClipboard, ToastLength.Short).Show();
        }

        private void AuthOptionsClick(object sender, int e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetItems(Resource.Array.authContextMenu, (alertSender, args) =>
            {
                switch(args.Which)
                {
                    case 0:
                        OpenRenameDialog(e);
                        break;

                    case 1:
                        ConfirmDelete(e);
                        break;
                }
            });

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void ConfirmDelete(int authNum)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirmDelete);
            builder.SetPositiveButton(Resource.String.delete, (sender, args) =>
            {
                _authSource.DeleteNth(authNum);
                _authAdapter.NotifyItemRemoved(authNum);
            });
            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });
            builder.SetCancelable(true);

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void FloatingActionButtonClick(object sender, System.EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, _floatingActionButton);
            menu.Inflate(Resource.Menu.add);
            menu.MenuItemClick += Fab_MenuItemClick;
            menu.Show();
        }

        private void Fab_MenuItemClick(object sender, PopupMenu.MenuItemClickEventArgs e)
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
                _database.Connection.Insert(auth);
            }
            catch
            {
                Toast.MakeText(this, Resource.String.qrCodeFormatError, ToastLength.Short).Show();
            }
        }

        private void ShowSortDialog()
        {
            AlertDialog.Builder sortDialog = new AlertDialog.Builder(this);
            sortDialog.SetTitle(Resource.String.sort)
            .SetItems(Resource.Array.sortTypes, (sender, e) =>
            {
                switch(e.Which)
                {
                    case 0: _authSource.Sort = AuthSource.SortType.Alphabetical; break;
                    case 1: _authSource.Sort = AuthSource.SortType.CreatedDate; break;
                }

                _authSource.ClearCache();
                _authAdapter.NotifyDataSetChanged();
            })
            .Create()
            .Show();
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
            _addDialog = new AddDialog(AddDialogPositive, AddDialogNegative);
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

            Authenticator auth = new Authenticator() {
                Issuer = issuer,
                Username = username,
                Type = type,
                Algorithm = algorithm,
                Counter = 0,
                Secret = secret,
                Digits = _addDialog.Digits,
                Period = _addDialog.Period
            };
            _database.Connection.Insert(auth);
            _addDialog.Dismiss();
        }

        private void AddDialogNegative(object sender, EventArgs e)
        {
            _addDialog.Dismiss();
        }

        /*
         *  Rename Dialog
         */
        private void OpenRenameDialog(int authPosition)
        {
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            Authenticator auth = _authSource.GetNth(authPosition);
            _renameDialog = new RenameDialog(RenameDialogPositive, RenameDialogNegative, auth);
            _renameDialog.Show(transaction, "rename_dialog");
        }

        private void RenameDialogPositive(object sender, EventArgs e)
        {
            if(_renameDialog.Issuer.Trim() == "")
            {
                _renameDialog.IssuerError = GetString(Resource.String.noIssuer);
                return;
            }

            string issuer = _renameDialog.Issuer.Trim().Truncate(32);
            string username = _renameDialog.Username.Trim().Truncate(32);

            _renameDialog.Authenticator.Issuer = issuer;
            _renameDialog.Authenticator.Username = username;

            _database.Connection.Update(_renameDialog.Authenticator);
            _authSource.ClearCache();
            _renameDialog?.Dismiss();
        }

        private void RenameDialogNegative(object sender, EventArgs e)
        {
            _renameDialog.Dismiss();
        }
    }
}

