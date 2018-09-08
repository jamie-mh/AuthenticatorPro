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
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, Icon = "@mipmap/ic_launcher")]
    // ReSharper disable once UnusedMember.Global
    public class MainActivity : AppCompatActivity
    {
        private Timer _timer;
        private RecyclerView _list;
        private FloatingActionButton _fab;
        private GeneratorAdapter _adapter;
        private GeneratorSource _genSource;
        private Database _db;
        private MobileBarcodeScanner _scanner;
        private DrawerLayout _drawerLayout;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.app_name);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_menu);

            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawerLayout);

            _fab = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _fab.Click += Fab_Click;

            MobileBarcodeScanner.Initialize(Application);
            _scanner = new MobileBarcodeScanner();

            SetupGeneratorList();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Android.Resource.Id.Home:
                    _drawerLayout.OpenDrawer(GravityCompat.Start);
                    break;

                case Resource.Id.action_settings:
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
            _list = FindViewById<RecyclerView>(Resource.Id.generatorList);

            _db = new Database(this);
            _genSource = new GeneratorSource(_db.Connection);
            _adapter = new GeneratorAdapter(_genSource);
            _adapter.ItemClick += this.GeneratorClick;
            _adapter.ItemOptionsClick += this.GeneratorOptionsClick;

            _list.SetAdapter(_adapter);
            _list.SetLayoutManager(new LinearLayoutManager(this));

            _timer = new Timer()
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += this.GeneratorTick;
            _timer.Start();
        }

        private void GeneratorTick(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
            });
        }

        private void GeneratorClick(object sender, int e)
        {
            ClipboardManager clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            Generator gen = _genSource.GetNth(e);
            ClipData clip = ClipData.NewPlainText("code", gen.Code);
            clipboard.PrimaryClip = clip;

            Snackbar.Make(_drawerLayout, Resource.String.code_copied_to_clipboard, Snackbar.LengthShort).Show();
        }

        private void GeneratorOptionsClick(object sender, int e)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetItems(Resource.Array.generator_options, (alertSender, args) =>
            {
                switch(args.Which)
                {
                    case 0:
                        break;

                    case 1:
                        ConfirmDelete(e);
                        break;
                }
            });

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void ConfirmDelete(int generator)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirm_delete);
            builder.SetPositiveButton(Resource.String.delete, (sender, args) =>
            {
                _genSource.DeleteNth(generator);
            });
            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });
            builder.SetCancelable(true);

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }

        private void Fab_Click(object sender, System.EventArgs e)
        {
            PopupMenu menu = new PopupMenu(this, _fab);
            menu.Inflate(Resource.Menu.menu_add);
            menu.MenuItemClick += this.Fab_MenuItemClick;
            menu.Show();
        }

        private void Fab_MenuItemClick(object sender, PopupMenu.MenuItemClickEventArgs e)
        {
            switch(e.Item.ItemId)
            {
                case Resource.Id.action_scan:
                    ScanQRCode();
                    break;

                case Resource.Id.action_enter_key:
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
                Generator gen = Generator.FromKeyUri(result.Text);
                _db.Connection.Insert(gen);
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
            AddFragment fragment = new AddFragment() {
                Arguments = null
            };

            fragment.Show(transaction, "add_dialog");
        }
    }
}

