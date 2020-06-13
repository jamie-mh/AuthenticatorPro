using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class LoginActivity : DayNightActivity
    {
        private const int RequestConfirmDeviceCredentials = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityLogin);

            var keyguardManager = (KeyguardManager) GetSystemService(KeyguardService);
            var loginIntent = keyguardManager.CreateConfirmDeviceCredentialIntent(GetString(Resource.String.login),
                GetString(Resource.String.loginMessage));
            StartActivityForResult(loginIntent, RequestConfirmDeviceCredentials);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if(requestCode != RequestConfirmDeviceCredentials)
                return;

            switch(resultCode)
            {
                case Result.Canceled:
                    FinishAffinity();
                    break;

                case Result.Ok:
                    Finish();
                    break;
            }
        }

        public override bool OnSupportNavigateUp()
        {
            return false;
        }

        public override void OnBackPressed()
        {

        }
    }
}