using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Albireo.Otp;
using ProAuth.Utilities;
using ProAuth.Data;

namespace ProAuth
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            toolbar.SetTitle(Resource.String.app_name);

            Database database = new Database();
            Generator gen = database.Connection.Get<Generator>(1);
            int code = Totp.GetCode(HashAlgorithm.Sha256, gen.secret, System.DateTime.Now);

            TextView text = FindViewById<TextView>(Resource.Id.textView1);
            text.Text = code.ToString();
        }
    }
}

