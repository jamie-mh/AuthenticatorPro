using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class PasswordSetupBottomSheet : BottomSheet
    {
        private PreferenceWrapper _preferences;
        private bool _hasPassword;

        private TextInputEditText _passwordText;
        private TextInputLayout _passwordConfirmLayout;
        private TextInputEditText _passwordConfirmText;
        private MaterialButton _cancelButton;
        private MaterialButton _setPasswordButton;
        
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetPasswordSetup, null);
            SetupToolbar(view, Resource.String.prefPasswordTitle);
            
            _preferences = new PreferenceWrapper(Context);
            _hasPassword = _preferences.PasswordProtected;
            
            _setPasswordButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSetPassword);
            _setPasswordButton.Click += OnSetPasswordButtonClick;

            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _passwordText.TextChanged += delegate { UpdateSetPasswordButton(); };
            
            _passwordConfirmLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordConfirmLayout);
            _passwordConfirmText = view.FindViewById<TextInputEditText>(Resource.Id.editPasswordConfirm);
            TextInputUtil.EnableAutoErrorClear(_passwordConfirmLayout);

            _passwordConfirmText.EditorAction += (_, e) =>
            {
                if(_setPasswordButton.Enabled && e.ActionId == ImeAction.Done)
                    OnSetPasswordButtonClick(null, null);
            };
            
            _cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            _cancelButton.Click += delegate { Dismiss(); };

            UpdateSetPasswordButton();
            return view;
        }

        private async void OnSetPasswordButtonClick(object sender, EventArgs e)
        {
            if(_passwordText.Text != _passwordConfirmText.Text)
            {
                _passwordConfirmLayout.Error = GetString(Resource.String.passwordsDoNotMatch);
                return;
            }

            var newPassword = _passwordText.Text == "" ? null : _passwordText.Text;

            _setPasswordButton.Enabled = _cancelButton.Enabled = false;
            _setPasswordButton.SetText(newPassword != null ? Resource.String.encrypting : Resource.String.decrypting);
            SetCancelable(false);

            try
            {
                var currentPassword = await SecureStorageWrapper.GetDatabasePassword();
                await Database.SetPassword(currentPassword, newPassword);

                try
                {
                    await SecureStorageWrapper.SetDatabasePassword(newPassword);
                }
                catch
                {
                    // Revert changes
                    await Database.SetPassword(newPassword, currentPassword);
                    throw;
                }
            }
            catch
            {
                Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short).Show();
                SetCancelable(true);
                UpdateSetPasswordButton();
                _cancelButton.Enabled = true;
                return;
            }

            _preferences.AllowBiometrics = false;
            _preferences.PasswordProtected = newPassword != null;
            _preferences.PasswordChanged = true;

            try
            {
                var manager = new PasswordStorageManager(Context);
                manager.Clear();
            }
            catch
            {
                // Not really an issue if this fails
            }
            
            var app = ((SettingsActivity) Context).BaseApplication;
            await app.Lock();
            await app.Unlock(newPassword);
            
            Dismiss();
        }

        private void UpdateSetPasswordButton()
        {
            var isEmpty = _passwordText.Text == "";
            
            if(!_hasPassword)
                _setPasswordButton.Enabled = !isEmpty;
            else
                _setPasswordButton.Enabled = true;

            if(_hasPassword && isEmpty)
                _setPasswordButton.SetText(Resource.String.clearPassword);
            else
                _setPasswordButton.SetText(Resource.String.setPassword);
        }
    }
}