// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Net;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Backup;
using AuthenticatorPro.Droid.Shared;

namespace AuthenticatorPro.Droid
{
    public class PreferenceWrapper : BasePreferenceWrapper
    {
        public PreferenceWrapper(Context context) : base(context)
        {
        }

        #region Standard preferences

        private const string ShowBackupRemindersKey = "pref_showBackupReminders";
        private const bool ShowBackupRemindersDefault = true;

        public bool ShowBackupReminders
        {
            get => Preferences.GetBoolean(ShowBackupRemindersKey, ShowBackupRemindersDefault);
            set => SetPreference(ShowBackupRemindersKey, value);
        }

        private const string LanguageKey = "pref_language";
        private const string LanguageDefault = "system";

        public string Language
        {
            get => Preferences.GetString(LanguageKey, LanguageDefault);
            set => SetPreference(LanguageKey, value);
        }

        private const string TapToCopyKey = "pref_tapToCopy";
        private const bool TapToCopyDefault = true;

        public bool TapToCopy
        {
            get => Preferences.GetBoolean(TapToCopyKey, TapToCopyDefault);
            set => SetPreference(TapToCopyKey, value);
        }

        private const string PasswordProtectedKey = "pref_passwordProtected";
        private const bool PasswordProtectedDefault = false;

        public bool PasswordProtected
        {
            get => Preferences.GetBoolean(PasswordProtectedKey, PasswordProtectedDefault);
            set => SetPreference(PasswordProtectedKey, value);
        }

        private const string AllowBiometricsKey = "pref_allowBiometrics";
        private const bool AllowBiometricsDefault = false;

        public bool AllowBiometrics
        {
            get => Preferences.GetBoolean(AllowBiometricsKey, AllowBiometricsDefault);
            set => SetPreference(AllowBiometricsKey, value);
        }

        private const string DatabasePasswordBackupKey = "pref_databasePasswordBackup";
        private const bool DatabasePasswordBackupDefault = false;

        public bool DatabasePasswordBackup
        {
            get => Preferences.GetBoolean(DatabasePasswordBackupKey, DatabasePasswordBackupDefault);
            set => SetPreference(DatabasePasswordBackupKey, value);
        }

        private const string TimeoutKey = "pref_timeout";
        private const int TimeoutDefault = 0;

        public int Timeout
        {
            get => GetStringBackedIntPreference(TimeoutKey, TimeoutDefault);
            set => SetPreference(TimeoutKey, value);
        }

        private const string TapToRevealKey = "pref_tapToReveal";
        private const bool TapToRevealDefault = false;

        public bool TapToReveal
        {
            get => Preferences.GetBoolean(TapToRevealKey, TapToRevealDefault);
            set => SetPreference(TapToRevealKey, value);
        }

        private const string TapToRevealDurationKey = "pref_tapToRevealDuration";
        private const int TapToRevealDurationDefault = 10;

        public int TapToRevealDuration
        {
            get => GetStringBackedIntPreference(TapToRevealDurationKey, TapToRevealDurationDefault);
            set => SetPreference(TapToRevealDurationKey, value);
        }

        private const string AllowScreenshotsKey = "pref_allowScreenshots";
        private const bool AllowScreenshotsDefault = false;

        public bool AllowScreenshots
        {
            get => Preferences.GetBoolean(AllowScreenshotsKey, AllowScreenshotsDefault);
            set => SetPreference(AllowScreenshotsKey, value);
        }

        private const string ThemeKey = "pref_theme";
        private const string ThemeDefault = "system";

        public string Theme
        {
            get => Preferences.GetString(ThemeKey, ThemeDefault);
            set => SetPreference(ThemeKey, value);
        }

        private const string ViewModeKey = "pref_viewMode";
        private const string ViewModeDefault = "default";

        public string ViewMode
        {
            get => Preferences.GetString(ViewModeKey, ViewModeDefault);
            set => SetPreference(ViewModeKey, value);
        }

        private const string CodeGroupSizeKey = "pref_codeGroupSize";
        private const int CodeGroupSizeDefault = 3;

        public int CodeGroupSize
        {
            get => GetStringBackedIntPreference(CodeGroupSizeKey, CodeGroupSizeDefault);
            set => SetPreference(CodeGroupSizeKey, value.ToString());
        }

        private const string ShowUsernamesKey = "pref_showUsernames";
        private const bool ShowUsernamesDefault = true;

        public bool ShowUsernames
        {
            get => Preferences.GetBoolean(ShowUsernamesKey, ShowUsernamesDefault);
            set => SetPreference(ShowUsernamesKey, value);
        }

        private const string DynamicColourKey = "pref_dynamicColour";
        private const bool DynamicColourDefault = false;

        public bool DynamicColour
        {
            get => Preferences.GetBoolean(DynamicColourKey, DynamicColourDefault);
            set => SetPreference(DynamicColourKey, value);
        }

        private const string AccentColourKey = "pref_accentColour";
        private const string AccentColourDefault = "lightBlue";

        public string AccentColour
        {
            get => Preferences.GetString(AccentColourKey, AccentColourDefault);
            set => SetPreference(AccentColourKey, value);
        }

        private const string TransparentStatusBarKey = "pref_transparentStatusBar";
        private const bool TransparentStatusBarDefault = false;

        public bool TransparentStatusBar
        {
            get => Preferences.GetBoolean(TransparentStatusBarKey, TransparentStatusBarDefault);
            set => SetPreference(TransparentStatusBarKey, value);
        }

        private const string AutoBackupEnabledKey = "pref_autoBackupEnabled";
        private const bool AutoBackupEnabledDefault = false;

        public bool AutoBackupEnabled
        {
            get => Preferences.GetBoolean(AutoBackupEnabledKey, AutoBackupEnabledDefault);
            set => SetPreference(AutoBackupEnabledKey, value);
        }

        private const string AutoRestoreEnabledKey = "pref_autoRestoreEnabled";
        private const bool AutoRestoreEnabledDefault = false;

        public bool AutoRestoreEnabled
        {
            get => Preferences.GetBoolean(AutoRestoreEnabledKey, AutoRestoreEnabledDefault);
            set => SetPreference(AutoRestoreEnabledKey, value);
        }

        private const string AutoBackupUriKey = "pref_autoBackupUri";
        private const Uri AutoBackupUriDefault = null;

        public Uri AutoBackupUri
        {
            get => GetUriPreference(AutoBackupUriKey, AutoBackupUriDefault);
            set => SetUriPreference(AutoBackupUriKey, value);
        }

        private const string AutoBackupPasswordProtectedKey = "pref_autoBackupPasswordProtected";
        private static readonly bool? AutoBackupPasswordProtectedDefault = null;

        public bool? AutoBackupPasswordProtected
        {
            get => GetNullableBooleanPreference(AutoBackupPasswordProtectedKey, AutoBackupPasswordProtectedDefault);
            set => SetNullableBooleanPreference(AutoBackupPasswordProtectedKey, value);
        }

        private const string SortModeKey = "pref_sortMode";
        private const SortMode SortModeDefault = SortMode.AlphabeticalAscending;

        public SortMode SortMode
        {
            get => GetEnumPreference(SortModeKey, SortModeDefault);
            set => SetEnumPreference(SortModeKey, value);
        }

        #endregion

        #region State

        private const string FirstLaunchKey = "firstLaunch";
        private const bool FirstLaunchDefault = true;

        public bool FirstLaunch
        {
            get => Preferences.GetBoolean(FirstLaunchKey, FirstLaunchDefault);
            set => SetPreference(FirstLaunchKey, value);
        }

        private const string ShownAutoTimeWarningKey = "shownAutoTimeWarning";
        private const bool ShownAutoTimeWarningDefault = false;

        public bool ShownAutoTimeWarning
        {
            get => Preferences.GetBoolean(ShownAutoTimeWarningKey, ShownAutoTimeWarningDefault);
            set => SetPreference(ShownAutoTimeWarningKey, value);
        }

        private const string DefaultCategoryKey = "defaultCategory";
        private const string DefaultCategoryDefault = null;

        public string DefaultCategory
        {
            get => Preferences.GetString(DefaultCategoryKey, DefaultCategoryDefault);
            set => SetPreference(DefaultCategoryKey, value);
        }

        private const string PasswordChangedKey = "passwordChanged";
        private const bool PasswordChangedDefault = false;

        public bool PasswordChanged
        {
            get => Preferences.GetBoolean(PasswordChangedKey, PasswordChangedDefault);
            set => SetPreference(PasswordChangedKey, value);
        }

        private const string BackupRequirementKey = "backupRequirement";
        private const BackupRequirement BackupRequirementDefault = BackupRequirement.NotRequired;

        public BackupRequirement BackupRequired
        {
            get => GetEnumPreference(BackupRequirementKey, BackupRequirementDefault);
            set => SetEnumPreference(BackupRequirementKey, value);
        }

        private const string AutoBackupTriggerKey = "autoBackupTrigger";
        private const bool AutoBackupTriggerDefault = false;

        public bool AutoBackupTrigger
        {
            get => Preferences.GetBoolean(AutoBackupTriggerKey, AutoBackupTriggerDefault);
            set => SetPreference(AutoBackupTriggerKey, value);
        }

        #endregion
    }
}