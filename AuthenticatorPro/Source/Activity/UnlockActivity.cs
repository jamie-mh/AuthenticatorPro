using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.Content;
using AuthenticatorPro.Callback;
using AuthenticatorPro.Data;
using AuthenticatorPro.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using BiometricManager = AndroidX.Biometric.BiometricManager;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class UnlockActivity : BaseActivity
    {
        private const int MaxAttempts = 3;
        private int _failedAttempts;
        private bool _canUseBiometrics;

        private PreferenceWrapper _preferences;
        private BiometricPrompt _prompt;

        private LinearLayout _middleLayout;
        private MaterialButton _unlockButton;
        private MaterialButton _useBiometricsButton;
        private TextInputLayout _passwordLayout;
        private TextInputEditText _passwordText;
        
        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityUnlock);
            Window.SetSoftInputMode(SoftInput.AdjustResize);
            SetResult(Result.Canceled);

            _preferences = new PreferenceWrapper(this);

            _middleLayout = FindViewById<LinearLayout>(Resource.Id.layoutMiddle);
            
            _passwordLayout = FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);
            _passwordText = FindViewById<TextInputEditText>(Resource.Id.editPassword);
            TextInputUtil.EnableAutoErrorClear(_passwordLayout);

            _passwordText.EditorAction += (_, e) =>
            {
                if(e.ActionId == ImeAction.Done)
                    OnUnlockButtonClick(null, null);
            };

            _unlockButton = FindViewById<MaterialButton>(Resource.Id.buttonUnlock);
            _unlockButton.Click += OnUnlockButtonClick;
           
            if(_preferences.AllowBiometrics)
            {
                var biometricManager = BiometricManager.From(this);
                _canUseBiometrics = biometricManager.CanAuthenticate() == BiometricManager.BiometricSuccess;
            }
            
            _useBiometricsButton = FindViewById<MaterialButton>(Resource.Id.buttonUseBiometrics);
            _useBiometricsButton.Enabled = _canUseBiometrics;
            _useBiometricsButton.Click += delegate
            {
                ShowBiometricPrompt();
            };

            if(_canUseBiometrics)
                ShowBiometricPrompt();
            else
                await FocusPasswordText();
        }

        private async void OnUnlockButtonClick(object sender, EventArgs e)
        {
            _unlockButton.Enabled = _useBiometricsButton.Enabled = false;
            await AttemptUnlock(_passwordText.Text);
            _unlockButton.Enabled = true;
            _useBiometricsButton.Enabled = _canUseBiometrics;
        }

        private async Task FocusPasswordText()
        {
            await Task.Run(async delegate
            {
                await Task.Delay(250);
                
                RunOnUiThread(delegate
                {
                    _passwordText.RequestFocus();
                    var inputManager = (InputMethodManager) GetSystemService(InputMethodService);
                    inputManager.ShowSoftInput(_middleLayout, ShowFlags.Implicit);
                });
            });
        }

        private async Task AttemptUnlock(string password)
        {
            try
            {
                await BaseApplication.Unlock(password);
            }
            catch
            {
                _passwordLayout.Error = GetString(Resource.String.passwordIncorrect);

                if(_failedAttempts > MaxAttempts)
                {
                    Toast.MakeText(this, Resource.String.tooManyAttempts, ToastLength.Short).Show();
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
            var passwordStorage = new PasswordStorageManager(this);
            var callback = new AuthenticationCallback();
            
            callback.Success += async (_, result) =>
            {
                string password;

                try
                {
                    password = passwordStorage.Fetch(result.CryptoObject.Cipher);
                }
                catch
                {
                    Toast.MakeText(this, Resource.String.genericError, ToastLength.Short);
                    return;
                }
                
                await AttemptUnlock(password);
            };

            callback.Failed += async delegate
            {
                await FocusPasswordText();
            };

            callback.Error += async (_, result) => 
            {
                Toast.MakeText(this, result.Message, ToastLength.Short).Show();
                await FocusPasswordText();
            };
            
            _prompt = new BiometricPrompt(this, executor, callback);
            
            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.unlock))
                .SetSubtitle(GetString(Resource.String.unlockBiometricsMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .SetConfirmationRequired(false)
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