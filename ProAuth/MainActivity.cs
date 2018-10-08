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
using Result = ZXing.Result;
using System;
using OtpSharp;

namespace ProAuth
{
    [Activity(Label = "@string/appName", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    // ReSharper disable once UnusedMember.Global
    public class MainActivity : AppCompatActivity
    {
        private Timer _authTimer;
        private RecyclerView _authList;
        private FloatingActionButton _floatingActionButton;
        private AuthAdapter _authAdapter;
        private AuthSource _authSource;
        private Database _database;
        private MobileBarcodeScanner _barcodeScanner;

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

            _floatingActionButton = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _floatingActionButton.Click += FloatingActionButtonClick;

            MobileBarcodeScanner.Initialize(Application);
            _barcodeScanner = new MobileBarcodeScanner();

            _database = new Database(this);

            StartActivity(typeof(LoginActivity));
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

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Resource.Id.actionSettings:
                    StartActivity(typeof(SettingsActivity));
                    break;

                case Resource.Id.actionImport:
                    StartActivity(typeof(ImportActivity));
                    break;

                case Resource.Id.actionExport:
                    StartActivity(typeof(ExportActivity));
                    break;

                case Resource.Id.actionAbout:
                    StartActivity(typeof(AboutActivity));
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

            Result result = await _barcodeScanner.Scan(options);

            if(result == null)
            {
                return;
            }

            Authenticator auth = Authenticator.FromKeyUri(result.Text);
            _database.Connection.Insert(auth);
        }

        /*
         *  Add Dialog
         */
        private void OpenAddDialog()
        {
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            Fragment old = FragmentManager.FindFragmentByTag("add_dialog");

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
            if(_addDialog.Issuer.Trim() == "")
            {
                Toast.MakeText(_addDialog.Context, Resource.String.noIssuer, ToastLength.Short).Show();
                return;
            }

            if(_addDialog.Secret.Trim() == "")
            {
                Toast.MakeText(_addDialog.Context, Resource.String.noSecret, ToastLength.Short).Show();
                return;
            }

            if(_addDialog.Secret.Trim().Length > 32)
            {
                Toast.MakeText(_addDialog.Context, Resource.String.secretTooLong, ToastLength.Short).Show();
                return;
            }

            if(_addDialog.Digits < 1)
            {
                Toast.MakeText(_addDialog.Context, Resource.String.digitsToSmall, ToastLength.Short).Show();
                return;
            }

            if(_addDialog.Period < 1)
            {
                Toast.MakeText(_addDialog.Context, Resource.String.periodToShort, ToastLength.Short).Show();
                return;
            }

            string issuer = _addDialog.Issuer.Trim().Truncate(32);
            string username = _addDialog.Username.Trim().Truncate(32);
            string secret = _addDialog.Secret.Trim();

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

            Authenticator auth = new Authenticator() {
                Issuer = issuer,
                Username = username,
                Type = OtpType.Totp,
                Algorithm = algorithm,
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
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            Fragment old = FragmentManager.FindFragmentByTag("rename_dialog");

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
                Toast.MakeText(_renameDialog.Context, Resource.String.noIssuer, ToastLength.Short).Show();
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

