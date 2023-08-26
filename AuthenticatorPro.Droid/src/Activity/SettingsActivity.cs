// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Biometric;
using AndroidX.Core.Content;
using AndroidX.Preference;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Interface.Fragment;
using AuthenticatorPro.Droid.Interface.Preference;
using AuthenticatorPro.Droid.Storage;
using Javax.Crypto;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    public class SettingsActivity : SensitiveSubActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private readonly IAuthenticatorService _authenticatorService;

        private PreferenceWrapper _preferences;
        private SecureStorageWrapper _secureStorageWrapper;

        private SettingsFragment _fragment;
        private bool _shouldRecreateMain;
        private bool _arePreferencesReady;

        public SettingsActivity() : base(Resource.Layout.activitySettings)
        {
            _authenticatorService = Dependencies.Resolve<IAuthenticatorService>();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            switch (key)
            {
                case "pref_language":
                case "pref_theme":
                case "pref_dynamicColour":
                case "pref_accentColour":
                    _shouldRecreateMain = true;
                    Recreate();
                    break;

                case "pref_tapToRevealDuration":
                case "pref_allowScreenshots":
                case "pref_viewMode":
                case "pref_codeGroupSize":
                case "pref_showUsernames":
                case "pref_transparentStatusBar":
                    _shouldRecreateMain = true;
                    break;

                case "pref_tapToReveal":
                    _shouldRecreateMain = true;
                    UpdateTapToRevealState();
                    break;

                case "pref_autoBackupEnabled":
                    UpdateBackupRemindersState();
                    break;

                case "pref_passwordProtected":
                case "passwordChanged":
                    _shouldRecreateMain = true;
                    UpdatePasswordState();
                    break;
            }
        }

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
                _arePreferencesReady = true;
                UpdateBackupRemindersState();
                UpdatePasswordState();
                UpdateTapToRevealState();
                UpdateDynamicColourEnabled();
                RegisterClickableEvents();
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

        private void RegisterClickableEvents()
        {
            var autoBackup = _fragment.FindPreference("pref_autoBackup");
            var resetCopyCount = _fragment.FindPreference("pref_resetCopyCount");
            var password = _fragment.FindPreference("pref_password");
            var biometrics = (MaterialSwitchPreference) _fragment.FindPreference("pref_allowBiometrics");

            autoBackup.PreferenceClick += delegate
            {
                var fragment = new AutoBackupSetupBottomSheet();
                fragment.Show(SupportFragmentManager, fragment.Tag);
            };

            resetCopyCount.PreferenceClick += async delegate
            {
                try
                {
                    await _authenticatorService.ResetCopyCountsAsync();
                    Toast.MakeText(this, Resource.String.copyCountReset, ToastLength.Short).Show();
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    Toast.MakeText(this, Resource.String.genericError, ToastLength.Short).Show();
                }
            };

            password.PreferenceClick += delegate
            {
                var fragment = new PasswordSetupBottomSheet();
                fragment.Show(SupportFragmentManager, fragment.Tag);
            };

            biometrics.PreferenceChange += (_, args) =>
            {
                var enabled = (bool) args.NewValue;

                if (enabled)
                {
                    EnableBiometrics(success =>
                    {
                        _preferences.AllowBiometrics = success;
                        biometrics.Checked = success;

                        if (!success)
                        {
                            ClearBiometrics();
                        }
                    });
                }
                else
                {
                    ClearBiometrics();
                    biometrics.Checked = false;
                    _preferences.AllowBiometrics = false;
                }
            };
        }

        #region Preference states

        private void UpdateBackupRemindersState()
        {
            if (_arePreferencesReady)
            {
                _fragment.FindPreference("pref_showBackupReminders").Enabled = !_preferences.AutoBackupEnabled;
            }
        }

        private void UpdatePasswordState()
        {
            if (!_arePreferencesReady)
            {
                return;
            }

            var biometrics = (MaterialSwitchPreference) _fragment.FindPreference("pref_allowBiometrics");
            biometrics.Enabled = _preferences.PasswordProtected && CanUseBiometrics();
            biometrics.Checked = _preferences.AllowBiometrics;

            _fragment.FindPreference("pref_timeout").Enabled = _preferences.PasswordProtected;
            _fragment.FindPreference("pref_databasePasswordBackup").Enabled = _preferences.PasswordProtected;
        }

        private void UpdateTapToRevealState()
        {
            if (_arePreferencesReady)
            {
                _fragment.FindPreference("pref_tapToRevealDuration").Enabled = _preferences.TapToReveal;
            }
        }

        private void UpdateDynamicColourEnabled()
        {
            if (!_arePreferencesReady)
            {
                return;
            }

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

        private void EnableBiometrics(Action<bool> callback)
        {
            var passwordStorage = new BiometricStorage(this);
            var executor = ContextCompat.GetMainExecutor(this);
            var authCallback = new AuthenticationCallback();

            authCallback.Succeeded += (_, result) =>
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