using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Albireo.Otp;
using ProAuth.Utilities;
using ProAuth.Data;
using System.Threading.Tasks;
using System.Timers;
using Android.Support.V7.Widget;
using Android.Util;

namespace ProAuth
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    // ReSharper disable once UnusedMember.Global
    public class MainActivity : AppCompatActivity
    {
        private Timer _timer;
        private RecyclerView _list;
        private GeneratorAdapter _adapter;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            toolbar.SetTitle(Resource.String.app_name);

            _list = FindViewById<RecyclerView>(Resource.Id.generatorList);

            Database db = new Database(this);
            GeneratorSource genSource = new GeneratorSource(db.Connection);
            ImplementationSource implSource = new ImplementationSource(db.Connection);

            _adapter = new GeneratorAdapter(genSource, implSource);
            _list.SetAdapter(_adapter);
            _list.SetLayoutManager(new LinearLayoutManager(this));

            DividerItemDecoration dividerItemDecoration = new DividerItemDecoration(_list.Context, LinearLayoutManager.Vertical);
            _list.AddItemDecoration(dividerItemDecoration);

            _timer = new Timer()
            {
                Interval = 1000,
                AutoReset = true,
                Enabled = true
            };

            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
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

