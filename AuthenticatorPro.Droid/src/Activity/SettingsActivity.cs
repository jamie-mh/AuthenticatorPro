// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Preference;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Interface.Fragment;
using AuthenticatorPro.Droid.Preference;
using AuthenticatorPro.Droid.Storage;
using Javax.Crypto;
using System;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class SettingsActivity : SensitiveSubActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private PreferenceWrapper _preferences;
        private SecureStorageWrapper _secureStorageWrapper;
        
        private SettingsFragment _fragment;
        private bool _shouldRecreateMain;
        private bool _ignoreBiometricsChange;

        public SettingsActivity() : base(Resource.Layout.activitySettings) { }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // If a setting that requires changes to the main activity has changed
            // return a result telling it to recreate.
            _shouldRecreateMain =
                savedInstanceState != null && savedInstanceState.GetBoolean("shouldRecreateMain", false);
            
            _preferences = new PreferenceWrapper(this);
            _secureStorageWrapper = new SecureStorageWrapper(this);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.settings);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            prefs.RegisterOnSharedPreferenceChangeListener(this);

            _fragment = new SettingsFragment();
            _fragment.PreferencesCreated += delegate
            {
                UpdateBackupRemindersEnabled();
                UpdateSecuritySettingsEnabled();
                UpdateDynamicColourEnabled();
            };

            // If all fingerprints have been removed the biometrics setting is still checked
            // In case of invalid biometrics, clear the key
            if (_preferences.AllowBiometrics && (!CanUseBiometrics() || IsBiometricsInvalidatedOrUnrecoverable()))
            {
                ClearBiometrics();
                _preferences.AllowBiometrics = false;
            }

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.layoutFragment, _fragment)
                .Commit();
        }

        public override void Finish()
        {
            if (_shouldRecreateMain)
            {
                SetResult(Result.Ok, null);
            }

            base.Finish();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            switch (key)
            {
                case "passwordChanged":
                    _preferences.PasswordChanged = false;
                    UpdateSecuritySettingsEnabled();
                    _shouldRecreateMain = true;
                    break;

                case "pref_theme":
                    UpdateTheme();
                    break;

                case "pref_language":
                case "pref_dynamicColour":
                case "pref_accentColour":
                    _shouldRecreateMain = true;
                    Recreate();
                    break;

                case "pref_tapToReveal":
                case "pref_viewMode":
                case "pref_codeGroupSize":
                case "pref_transparentStatusBar":
                    _shouldRecreateMain = true;
                    break;

                case "pref_autoBackupEnabled":
                    UpdateBackupRemindersEnabled();
                    break;

                case "pref_allowBiometrics":
                    HandleBiometricsChange();
                    break;
            }
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean("shouldRecreateMain", _shouldRecreateMain);
        }

        #region Preference states

        private void UpdateBackupRemindersEnabled()
        {
            _fragment.FindPreference("pref_showBackupReminders").Enabled = !_preferences.AutoBackupEnabled;
        }

        private void UpdateSecuritySettingsEnabled()
        {
            if (!_preferences.PasswordProtected)
            {
                _fragment.FindPreference("pref_allowBiometrics").Enabled = false;
                _fragment.FindPreference("pref_timeout").Enabled = false;
                _fragment.FindPreference("pref_databasePasswordBackup").Enabled = false;
                return;
            }

            _fragment.FindPreference("pref_allowBiometrics").Enabled = CanUseBiometrics();
            _fragment.FindPreference("pref_timeout").Enabled = true;
            _fragment.FindPreference("pref_databasePasswordBackup").Enabled = true;
        }
        
        private void UpdateDynamicColourEnabled()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                _fragment.FindPreference("pref_dynamicColour").Enabled = true;
                _fragment.FindPreference("pref_accentColour").Enabled = !_preferences.DynamicColour;
            }
            else
            {
                _fragment.FindPreference("pref_dynamicColour").Enabled = false;
                _fragment.FindPreference("pref_accentColour").Enabled = true;
                _preferences.DynamicColour = false;
            }
        }

        #endregion

        #region Biometrics

        private bool CanUseBiometrics()
        {
            var biometricManager = BiometricManager.From(this);
            return biometricManager.CanAuthenticate(BiometricManager.Authenticators.BiometricStrong) ==
                BiometricManager.BiometricSuccess;
        }

        private bool IsBiometricsInvalidatedOrUnrecoverable()
        {
            var passwordStorage = new BiometricStorage(this);

            try
            {
                _ = passwordStorage.GetDecryptionCipher();
            }
            catch (Exception e)
            {
                Logger.Error("Key invalidated or unrecoverable", e);
                return true;
            }

            return false;
        }
        
        private void HandleBiometricsChange()
        {
            if (_ignoreBiometricsChange)
            {
                _ignoreBiometricsChange = false;
                return;
            }
            
            var preference = (MaterialSwitchPreference) _fragment.FindPreference("pref_allowBiometrics");

            if (preference == null)
            {
                return;
            }
            
            if (!preference.Checked)
            {
                ClearBiometrics();
            }
            else
            {
                EnableBiometrics(success =>
                {
                    if (!success)
                    {
                        _ignoreBiometricsChange = true;
                        _preferences.AllowBiometrics = false;
                        preference.Checked = false;
                        ClearBiometrics();
                    }
                });
            }
        }

        private void EnableBiometrics(Action<bool> callback)
        {
            var passwordStorage = new BiometricStorage(this);
            var executor = ContextCompat.GetMainExecutor(this);
            var authCallback = new AuthenticationCallback();

            authCallback.Succeeded += async (_, result) =>
            {
                try
                {
                    var password = _secureStorageWrapper.GetDatabasePassword();
                    passwordStorage.Store(password, result.CryptoObject.Cipher);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    callback(false);
                    return;
                }

                callback(true);
            };

            authCallback.Failed += delegate
            {
                Toast.MakeText(this, Resource.String.genericError, ToastLength.Long).Show();
                callback(false);
            };

            authCallback.Errored += (_, args) =>
            {
                Toast.MakeText(this, args.Message, ToastLength.Long).Show();
                callback(false);
            };

            var prompt = new BiometricPrompt(this, executor, authCallback);

            var promptInfo = new BiometricPrompt.PromptInfo.Builder()
                .SetTitle(GetString(Resource.String.setupBiometricUnlock))
                .SetNegativeButtonText(GetString(Resource.String.cancel))
                .SetConfirmationRequired(false)
                .SetAllowedAuthenticators(BiometricManager.Authenticators.BiometricStrong)
                .Build();

            Cipher cipher;

            try
            {
                cipher = passwordStorage.GetEncryptionCipher();
            }
            catch (Exception e)
            {
                Logger.Error(e);
                passwordStorage.Clear();
                callback(false);
                return;
            }

            prompt.Authenticate(promptInfo, new BiometricPrompt.CryptoObject(cipher));
        }

        private void ClearBiometrics()
        {
            var storage = new BiometricStorage(this);
            storage.Clear();
        }

        #endregion
    }
}