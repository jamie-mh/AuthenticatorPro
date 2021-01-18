using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AuthenticatorPro.Callback;
using AuthenticatorPro.Data;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class LoginActivity : DayNightActivity
    {
        private const int MaxAttempts = 3;
        private int _failedAttempts;

        private PreferenceWrapper _preferences;
        private BiometricPrompt _prompt;
        private DatabasePasswordStorage _passwordStorage; 
        private TextInputEditText _passwordText;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityLogin);

            _preferences = new PreferenceWrapper(this);

            SetResult(Result.Canceled);
            _passwordText = FindViewById<TextInputEditText>(Resource.Id.editPassword);

            // TODO: perhaps rename to 'unlock'
            var loginButton = FindViewById<MaterialButton>(Resource.Id.buttonLogin);
            loginButton.Click += OnLoginButtonClick;
           
            var canUseBiometrics = false;
            
            if(_preferences.AllowBiometrics)
            {
                var biometricManager = BiometricManager.From(this);
                canUseBiometrics = biometricManager.CanAuthenticate() == BiometricManager.BiometricSuccess;
            }
            
            var useBiometricsButton = FindViewById<MaterialButton>(Resource.Id.buttonUseBiometrics);
            useBiometricsButton.Visibility = canUseBiometrics ? ViewStates.Visible : ViewStates.Gone;
            useBiometricsButton.Click += delegate
            {
                ShowBiometricPrompt();
            };
            
            if(canUseBiometrics)
                ShowBiometricPrompt();
        }

        private async void OnLoginButtonClick(object sender, EventArgs e)
        {
            // TODO: disable button until attempt is completed
            var password = _passwordText.Text;
            await AttemptLogin(password);
        }

        private async Task AttemptLogin(string password)
        {
            try
            {
                await Database.OpenSharedConnection(password);
            }
            catch
            {
                Toast.MakeText(this, "wrong password", ToastLength.Short).Show();

                if(_failedAttempts > MaxAttempts)
                {
                    Toast.MakeText(this, "too many attempts", ToastLength.Short).Show();
                    Finish();
                    return;
                }
                
                _failedAttempts++;
                return;
            }
            
            SetResult(Result.Ok);
            Finish();
        }

        private void ShowBiometricPrompt()
        {
            var executor = ContextCompat.GetMainExecutor(this);
            var passwordStorage = new DatabasePasswordStorage(this);
            var callback = new AuthenticationCallback();
            
            callback.Success += async (_, result) =>
            {
                // TODO: try catch
                var password = passwordStorage.Fetch(result.CryptoObject.Cipher);
                await AttemptLogin(password);
            };

            callback.Failed += delegate
            {
                // Do something, probably
            };

            callback.Error += (_, result) => 
            {
                Toast.MakeText(this, result.Message, ToastLength.Long).Show();
            };
            
            _prompt = new BiometricPrompt(this, executor, callback);
            
            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.login))
                .SetSubtitle(GetString(Resource.String.loginMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .Build();
            
            var cipher = passwordStorage.GetDecryptionCipher();
            _prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
        }

        protected override void OnPause()
        {
            base.OnPause();
            _prompt?.CancelAuthentication();
            Finish();
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override void OnBackPressed()
        {
            _prompt?.CancelAuthentication();
            Finish();
        }
    }
}