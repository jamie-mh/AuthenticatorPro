using System.Collections.Generic;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using ProAuth.Utilities;
using System.Timers;
using Android.Content;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Support.V4.Widget;
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

namespace ProAuth
{
    [Activity(Label = "@string/appName", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    public class MainActivity : AppCompatActivity
    {
        private Timer _timer;
        private RecyclerView _list;
        private FloatingActionButton _fab;
        private AuthAdapter _adapter;
        private AuthSource _authSource;
        private Database _db;
        private MobileBarcodeScanner _scanner;
        private DrawerLayout _drawerLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityMain);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityMain_toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.appName);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_menu);

            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.activityMain_drawerLayout);

            _fab = FindViewById<FloatingActionButton>(Resource.Id.activityMain_buttonAdd);
            _fab.Click += Fab_Click;

            MobileBarcodeScanner.Initialize(Application);
            _scanner = new MobileBarcodeScanner();

            SetupGeneratorList();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Android.Resource.Id.Home:
                    _drawerLayout.OpenDrawer(GravityCompat.Start);
                    break;

                case Resource.Id.actionSettings:
                    Toast.MakeText (this, "You pressed settings action!", ToastLength.Short).Show ();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _timer.Stop();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _timer.Start();
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            _scanner.Cancel();
        }

        private void SetupGeneratorList()
        {
            _list = FindViewById<RecyclerView>(Resource.Id.activityMain_authList);

            _db = new Database(this);
            _authSource = new AuthSource(_db.Connection);
            _adapter = new AuthAdapter(_authSource);
            _adapter.ItemClick += this.AuthClick;
            _adapter.ItemOptionsClick += this.AuthOptionsClick;

            _list.SetAdapter(_adapter);
            _list.SetLayoutManager(new LinearLayoutManager(this));

            _timer = new Timer()
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += this.AuthTick;
            _timer.Start();
        }

        private void AuthTick(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
            });
        }

        private void AuthClick(object sender, int e)
        {
            ClipboardManager clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            Authenticator auth = _authSource.GetNth(e);
            ClipData clip = ClipData.NewPlainText("code", auth.Code);
            clipboard.PrimaryClip = clip;

            Snackbar.Make(_drawerLayout, Resource.String.copiedToClipboard, Snackbar.LengthShort).Show();
        }

        private void AuthOptionsClick(object sender, int e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetItems(Resource.Array.authContextMenu, (alertSender, args) =>
            {
                switch(args.Which)
                {
                    case 0:
                        break;

                    case 2:
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

        private void Fab_Click(object sender, System.EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, _fab);
            menu.Inflate(Resource.Menu.add);
            menu.MenuItemClick += this.Fab_MenuItemClick;
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

            Result result = await _scanner.Scan(options);

            if(result != null)
            {
                Authenticator auth = Authenticator.FromKeyUri(result.Text);
                _db.Connection.Insert(auth);
            }
        }

        private void OpenAddDialog()
        {
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            Fragment old = FragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            AddFragment fragment = new AddFragment(_db) {
                Arguments = null
            };

            fragment.Show(transaction, "add_dialog");
        }
    }
}

