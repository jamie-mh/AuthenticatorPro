using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
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

            _textPassword = FindViewById<EditText>(Resource.Id.activityLogin_password);

            Button loginButton = FindViewById<Button>(Resource.Id.activityLogin_login);
            loginButton.Click += LoginButton_Click;

            CrossFingerprint.SetCurrentActivityResolver(() => this);
            AuthenticationRequestConfiguration config =
                new AuthenticationRequestConfiguration("boi") {UseDialog = false};

            FingerprintAuthenticationResult result = 
                await CrossFingerprint.Current.AuthenticateAsync(config);
            if (result.Authenticated)
            {
                Finish();
            }
            else
            {
                Toast.MakeText(this, "failure", ToastLength.Short).Show();
            }
        }

        private void LoginButton_Click(object sender, System.EventArgs e)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            string correctPassword = prefs.GetString("password", "");

            if(_textPassword.Text == correctPassword)
            {
                Finish();
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