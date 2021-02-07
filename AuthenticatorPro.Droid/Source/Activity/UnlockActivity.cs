using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Core.Content;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Javax.Crypto;
using BiometricManager = AndroidX.Biometric.BiometricManager;
using BiometricPrompt = AndroidX.Biometric.BiometricPrompt;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class UnlockActivity : BaseActivity
    {
        private const int MaxAttempts = 3;
        private int _failedAttempts;
        private bool _canUseBiometrics;

        private PreferenceWrapper _preferences;
        private BiometricPrompt _prompt;

        private LinearLayout _unlockLayout;
        private MaterialButton _unlockButton;
        private MaterialButton _useBiometricsButton;
        private TextInputLayout _passwordLayout;
        private TextInputEditText _passwordText;
        
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityUnlock);
            Window.SetSoftInputMode(SoftInput.AdjustResize);
            SetResult(Result.Canceled);

            _preferences = new PreferenceWrapper(this);
            _unlockLayout = FindViewById<LinearLayout>(Resource.Id.layoutUnlock);
            
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
                FocusPasswordText();
        }

        private async void OnUnlockButtonClick(object sender, EventArgs e)
        {
            _unlockButton.Enabled = _useBiometricsButton.Enabled = false;
            await AttemptUnlock(_passwordText.Text);
            _unlockButton.Enabled = true;
            _useBiometricsButton.Enabled = _canUseBiometrics;
        }

        private void FocusPasswordText()
        {
            RunOnUiThread(delegate
            {
                if(_unlockLayout.Visibility == ViewStates.Visible)
                    return;
                
                AnimUtil.FadeInView(_unlockLayout, AnimUtil.LengthLong, true, delegate
                {
                    if(!_passwordText.RequestFocus())
                        return;
                        
                    var inputManager = (InputMethodManager) GetSystemService(InputMethodService);
                    inputManager.ShowSoftInput(_passwordText, ShowFlags.Implicit);
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

            callback.Failed += delegate
            {
                FocusPasswordText();
            };

            callback.Error += (_, result) => 
            {
                Toast.MakeText(this, result.Message, ToastLength.Short).Show();
                FocusPasswordText();
            };
            
            _prompt = new BiometricPrompt(this, executor, callback);
            
            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.unlock))
                .SetSubtitle(GetString(Resource.String.unlockBiometricsMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .SetConfirmationRequired(false)
                .Build();

            Cipher cipher;

            try
            {
                cipher = passwordStorage.GetDecryptionCipher();
            }
            catch
            {
                _canUseBiometrics = false;
                _useBiometricsButton.Enabled = false;
                return;
            }
            
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