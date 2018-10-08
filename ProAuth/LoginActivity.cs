using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace ProAuth
{
    [Activity(Label = "LoginActivity")]
    public class LoginActivity: AppCompatActivity
    {
        private EditText _textPassword;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityLogin);

            _textPassword = FindViewById<EditText>(Resource.Id.activityExport_password);

            CrossFingerprint.SetCurrentActivityResolver(() => this);
            AuthenticationRequestConfiguration config = new AuthenticationRequestConfiguration("boi");
            config.UseDialog = false;

            var result = await CrossFingerprint.Current.AuthenticateAsync(config);
            if (result.Authenticated)
            {
                Finish();
            }
            else
            {
                Toast.MakeText(this, "failure", ToastLength.Short).Show();
            }
        }

        public override bool OnSupportNavigateUp()
        {
            return false;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return false;
        }

        public override void OnBackPressed()
        {

        }
    }
}