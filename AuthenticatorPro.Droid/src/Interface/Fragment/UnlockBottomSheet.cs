// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using System;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class UnlockBottomSheet : BottomSheet
    {
        public event EventHandler<string> UnlockAttempted;

        private PreferenceWrapper _preferences;
        private BiometricPrompt _prompt;
        private bool _canUseBiometrics;

        private ProgressBar _progressBar;
        private MaterialButton _unlockButton;
        private MaterialButton _useBiometricsButton;
        private TextInputLayout _passwordLayout;
        private TextInputEditText _passwordText;

        public UnlockBottomSheet() : base(Resource.Layout.sheetUnlock)
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.unlock);

            _preferences = new PreferenceWrapper(Context);
            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            _passwordLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            TextInputUtil.EnableAutoErrorClear(_passwordLayout);

            _passwordText.EditorAction += (_, e) =>
            {
                if (e.ActionId == ImeAction.Done)
                {
                    UnlockAttempted?.Invoke(this, _passwordText.Text);
                }
            };

            _unlockButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUnlock);
            _unlockButton.Click += delegate
            {
                UnlockAttempted?.Invoke(this, _passwordText.Text);
            };

            if (_preferences.AllowBiometrics)
            {
                var biometricManager = BiometricManager.From(RequireContext());
                _canUseBiometrics = biometricManager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong) ==
                                    BiometricManager.BiometricSuccess;

                if (!_canUseBiometrics)
                {
                    Toast.MakeText(Context, Resource.String.biometricsChanged, ToastLength.Long).Show();
                }
            }

            _useBiometricsButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUseBiometrics);
            _useBiometricsButton.Enabled = _canUseBiometrics;
            _useBiometricsButton.Click += delegate
            {
                ShowBiometricPrompt();
            };

            if (_canUseBiometrics)
            {
                ShowBiometricPrompt();
            }
            else
            {
                FocusPasswordText();
            }

            return view;
        }

        public void SetBusy()
        {
            _unlockButton.Enabled = _useBiometricsButton.Enabled = false;
            _progressBar.Visibility = ViewStates.Visible;
        }

        private void ClearBusy()
        {
            _unlockButton.Enabled = true;
            _useBiometricsButton.Enabled = _canUseBiometrics;
            _progressBar.Visibility = ViewStates.Invisible;
        }

        public void ShowError()
        {
            ClearBusy();
            FocusPasswordText();
            _passwordLayout.Error = GetString(Resource.String.passwordIncorrect);
        }

        private void FocusPasswordText()
        {
            if (!_passwordText.RequestFocus())
            {
                return;
            }

            var inputManager = (InputMethodManager) Context.GetSystemService(Context.InputMethodService);
            inputManager.ShowSoftInput(_passwordText, ShowFlags.Implicit);
        }

        private void ShowBiometricPrompt()
        {
            var executor = ContextCompat.GetMainExecutor(RequireContext());
            var passwordStorage = new BiometricStorage(Context);
            var callback = new AuthenticationCallback();

            callback.Succeeded += (_, result) =>
            {
                string password;

                try
                {
                    password = passwordStorage.Fetch(result.CryptoObject.Cipher);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short);
                    return;
                }

                UnlockAttempted?.Invoke(this, password);
            };

            callback.Failed += delegate
            {
                FocusPasswordText();
            };

            callback.Errored += (_, result) =>
            {
                Toast.MakeText(Context, result.Message, ToastLength.Short).Show();
                FocusPasswordText();
            };

            _prompt = new BiometricPrompt(this, executor, callback);

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.unlock))
                .SetSubtitle(GetString(Resource.String.unlockBiometricsMessage))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .SetConfirmationRequired(false)
                .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricStrong)
                .Build();

            try
            {
                var cipher = passwordStorage.GetDecryptionCipher();
                _prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                _canUseBiometrics = false;
                _useBiometricsButton.Enabled = false;
                FocusPasswordText();
            }
        }

        public override void Dismiss()
        {
            base.Dismiss();
            _prompt?.CancelAuthentication();
        }
    }
}