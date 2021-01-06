using Android.App;
using Android.OS;
using Android.Widget;
using AndroidX.Core.Content;
using AuthenticatorPro.Callback;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class LoginActivity : DayNightActivity
    {
        private BiometricPrompt _prompt;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityLogin);

            var executor = ContextCompat.GetMainExecutor(this);
            var callback = new AuthenticationCallback();
            callback.Success += OnSuccess;
            callback.Error += OnError;
            
            _prompt = new BiometricPrompt(this, executor, callback);
            
            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.login))
                .SetSubtitle(GetString(Resource.String.loginMessage))
                .SetDeviceCredentialAllowed(true)
                .Build();
            
            // TODO: Test BiometricManager.CanAuthenticate when package is updated.
            
            _prompt.Authenticate(promptInfo);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _prompt?.CancelAuthentication();
        }

        private void OnError(object sender, AuthenticationCallback.ErrorEventArgs e)
        {
            Toast.MakeText(this, e.Message, ToastLength.Long).Show();
            FinishAffinity();
        }

        private void OnSuccess(object sender, BiometricPrompt.AuthenticationResult e)
        {
            SetResult(Result.Ok);
            Finish();
        }

        public override bool OnSupportNavigateUp()
        {
            return false;
        }

        public override void OnBackPressed()
        {
            _prompt?.CancelAuthentication();
        }
    }
}