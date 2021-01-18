using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AuthenticatorPro.Callback;
using AuthenticatorPro.Data;
using Google.Android.Material.Button;
using Google.Android.Material.SwitchMaterial;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Fragment
{
    internal class PasswordSetupBottomSheet : BottomSheet
    {
        private PreferenceWrapper _preferences;
        private TextInputEditText _passwordText;
        
        public PasswordSetupBottomSheet()
        {
            RetainInstance = true;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetPasswordSetup, null);
            SetupToolbar(view, Resource.String.password, true);
            _preferences = new PreferenceWrapper(Context);
            
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);

            var setPasswordButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSetPassword);
            // TODO: disable biometrics on password change
            // TODO: force recreate of main
            // TODO: lock ui until complete
            setPasswordButton.Click += OnSetPasswordButtonClick; 

            // TODO: check if has hw and enrolled
            // TODO: check if has password
            var switchBiometrics = view.FindViewById<SwitchMaterial>(Resource.Id.switchBiometricsEnabled);
            switchBiometrics.CheckedChange += OnSwitchBiometricsCheckedChange;

            return view;
        }

        private void OnSwitchBiometricsCheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if(!e.IsChecked)
            {
                _preferences.AllowBiometrics = false;
                return;
            }
            
            var passwordStorage = new DatabasePasswordStorage(Context);
            var executor = ContextCompat.GetMainExecutor(Context);
            var callback = new AuthenticationCallback();
            
            callback.Success += async (_, result) =>
            {
                var password = await SecureStorageWrapper.GetDatabasePassword();
                passwordStorage.Store(password, result.CryptoObject.Cipher);
                _preferences.AllowBiometrics = true;
            };

            // TODO: reset on cancel
            callback.Failed += delegate
            {
                
            };
            
            callback.Error += delegate
            {
                // Do something, probably
            };
            
            var prompt = new BiometricPrompt(this, executor, callback);
           
            // TODO: use specific strings for this purpose
            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.login))
                .SetSubtitle(GetString(Resource.String.loginMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .Build();

            var cipher = passwordStorage.GetEncryptionCipher();
            prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
        }

        private async void OnSetPasswordButtonClick(object sender, EventArgs e)
        {
            var newPassword = _passwordText.Text == "" ? null : _passwordText.Text;
            var currentPassword = await SecureStorageWrapper.GetDatabasePassword();

            try
            {
                await Database.SetPassword(currentPassword, newPassword);
                _preferences.PasswordProtected = newPassword != null;
                await SecureStorageWrapper.SetDatabasePassword(newPassword);
            }
            catch
            {
                Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short).Show();
            }
        }
    }
}