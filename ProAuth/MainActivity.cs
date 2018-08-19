using Android.App;
using Android.OS;
using Android.Support.V7.App;
using ProAuth.Utilities;
using System.Timers;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using ProAuth.Data;
using ZXing.Mobile;
using Result = ZXing.Result;

namespace ProAuth
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    // ReSharper disable once UnusedMember.Global
    public class MainActivity : AppCompatActivity
    {
        private Timer _timer;
        private RecyclerView _list;
        private GeneratorAdapter _adapter;
        private Database _db;
        private MobileBarcodeScanner _scanner;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            MobileBarcodeScanner.Initialize(Application);
            _scanner = new MobileBarcodeScanner();

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            toolbar.SetTitle(Resource.String.app_name);

            _list = FindViewById<RecyclerView>(Resource.Id.generatorList);

            _db = new Database(this);
            GeneratorSource genSource = new GeneratorSource(_db.Connection);

            _adapter = new GeneratorAdapter(genSource);
            _list.SetAdapter(_adapter);
            _list.SetLayoutManager(new LinearLayoutManager(this));

            _timer = new Timer()
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();

            FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            fab.Click += Fab_Click;
        }

        private async void Fab_Click(object sender, System.EventArgs e)
        {
            Result result = await _scanner.Scan();
            HandleCode(result);
        }

        private void HandleCode(Result result)
        {
            Generator gen = Generator.FromKeyUri(result.Text);
            _db.Connection.Insert(gen);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.menu_main, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) {
                case Resource.Id.action_settings:
                    Toast.MakeText (this, "You pressed settings action!", ToastLength.Short).Show ();
                    break;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
            });
        }
    }
}

