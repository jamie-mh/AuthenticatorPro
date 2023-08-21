// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Droid.Storage;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextField;
using System;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class PasswordSetupBottomSheet : BottomSheet
    {
        private readonly Database _database;
        private PreferenceWrapper _preferences;
        private SecureStorageWrapper _secureStorageWrapper;
        
        private bool _hasPassword;

        private TextInputEditText _passwordText;
        private TextInputLayout _passwordConfirmLayout;
        private TextInputEditText _passwordConfirmText;
        private MaterialButton _cancelButton;
        private MaterialButton _setPasswordButton;
        private CircularProgressIndicator _progressIndicator;

        public PasswordSetupBottomSheet() : base(Resource.Layout.sheetPasswordSetup, Resource.String.prefPasswordTitle)
        {
            _database = Dependencies.Resolve<Database>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _preferences = new PreferenceWrapper(RequireContext());
            _hasPassword = _preferences.PasswordProtected;

            _secureStorageWrapper = new SecureStorageWrapper(RequireContext());
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            
            _progressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.progressIndicator);

            _setPasswordButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSetPassword);
            _setPasswordButton.Click += OnSetPasswordButtonClick;

            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _passwordText.TextChanged += delegate { UpdateSetPasswordButton(); };

            _passwordConfirmLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordConfirmLayout);
            _passwordConfirmText = view.FindViewById<TextInputEditText>(Resource.Id.editPasswordConfirm);
            TextInputUtil.EnableAutoErrorClear(_passwordConfirmLayout);

            _passwordConfirmText.EditorAction += (_, e) =>
            {
                if (_setPasswordButton.Enabled && e.ActionId == ImeAction.Done)
                {
                    OnSetPasswordButtonClick(null, null);
                }
            };

            _cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            _cancelButton.Click += delegate { Dismiss(); };

            UpdateSetPasswordButton();
            return view;
        }
        
        private void SetLoading(bool loading)
        {
            SetCancelable(!loading);

            if (loading)
            {
                _setPasswordButton.Visibility = ViewStates.Invisible;
                _progressIndicator.Visibility = ViewStates.Visible;
                _cancelButton.Enabled = false;
            }
            else
            {
                _setPasswordButton.Visibility = ViewStates.Visible;
                _progressIndicator.Visibility = ViewStates.Invisible;
                _cancelButton.Enabled = true;
            }
        }

        private async void OnSetPasswordButtonClick(object sender, EventArgs args)
        {
            if (_passwordText.Text != _passwordConfirmText.Text)
            {
                _passwordConfirmLayout.Error = GetString(Resource.String.passwordsDoNotMatch);
                return;
            }

            var newPassword = _passwordText.Text == "" ? null : _passwordText.Text;
            SetLoading(true);

            try
            {
                var currentPassword = _secureStorageWrapper.GetDatabasePassword();
                await _database.SetPassword(currentPassword, newPassword);

                try
                {
                    _secureStorageWrapper.SetDatabasePassword(newPassword);
                }
                catch
                {
                    // Revert changes
                    await _database.SetPassword(newPassword, currentPassword);
                    throw;
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short).Show();
                UpdateSetPasswordButton();
                return;
            }
            finally
            {
                SetLoading(false);
            }

            _preferences.AllowBiometrics = false;
            _preferences.PasswordProtected = newPassword != null;
            _preferences.PasswordChanged = true;

            if (newPassword == null && _preferences.DatabasePasswordBackup)
            {
                _preferences.DatabasePasswordBackup = false;
            }

            try
            {
                var manager = new BiometricStorage(Context);
                manager.Clear();
            }
            catch (Exception e)
            {
                // Not really an issue if this fails
                Logger.Error(e);
            }

            Dismiss();
        }

        private void UpdateSetPasswordButton()
        {
            var isEmpty = _passwordText.Text == "";

            if (!_hasPassword)
            {
                _setPasswordButton.Enabled = !isEmpty;
            }
            else
            {
                _setPasswordButton.Enabled = true;
            }

            if (_hasPassword && isEmpty)
            {
                _setPasswordButton.SetText(Resource.String.clearPassword);
            }
            else
            {
                _setPasswordButton.SetText(Resource.String.setPassword);
            }
        }
    }
}