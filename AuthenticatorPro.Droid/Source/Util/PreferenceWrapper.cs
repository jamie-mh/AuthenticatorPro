using System;
using Android.Content;
using AndroidX.Preference;
using AuthenticatorPro.Droid.Data.Backup;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Util
{
    internal class PreferenceWrapper
    {
        #region Standard preferences
        private const string ShowBackupRemindersKey = "pref_showBackupReminders";
        private const bool ShowBackupRemindersDefault = true;
        public bool ShowBackupReminders 
        {
            get => _preferences.GetBoolean(ShowBackupRemindersKey, ShowBackupRemindersDefault);
            set => SetPreference(ShowBackupRemindersKey, value);
        }

        private const string PasswordProtectedKey = "pref_passwordProtected";
        private const bool PasswordProtectedDefault = false;
        public bool PasswordProtected
        {
            get => _preferences.GetBoolean(PasswordProtectedKey, PasswordProtectedDefault);
            set => SetPreference(PasswordProtectedKey, value);
        }
        
        private const string AllowBiometricsKey = "pref_allowBiometrics";
        private const bool AllowBiometricsDefault = false;
        public bool AllowBiometrics
        {
            get => _preferences.GetBoolean(AllowBiometricsKey, AllowBiometricsDefault);
            set => SetPreference(AllowBiometricsKey, value);
        }
        
        private const string DatabasePasswordBackupKey = "pref_databasePasswordBackup";
        private const bool DatabasePasswordBackupDefault = false;
        public bool DatabasePasswordBackup
        {
            get => _preferences.GetBoolean(DatabasePasswordBackupKey, DatabasePasswordBackupDefault);
            set => SetPreference(DatabasePasswordBackupKey, value);
        }
        
        private const string TimeoutKey = "pref_timeout";
        private const int TimeoutDefault = 0;
        public int Timeout
        {
            get => int.TryParse(_preferences.GetString(TimeoutKey, TimeoutDefault.ToString()), out var value) ? value : TimeoutDefault;
            set => SetPreference(TimeoutKey, value);
        }

        private const string ThemeKey = "pref_theme";
        private const string ThemeDefault = "system";
        public string Theme
        {
            get => _preferences.GetString(ThemeKey, ThemeDefault);
            set => SetPreference(ThemeKey, value);
        }
        
        private const string ViewModeKey = "pref_viewMode";
        private const string ViewModeDefault = "default";
        public string ViewMode
        {
            get => _preferences.GetString(ViewModeKey, ViewModeDefault);
            set => SetPreference(ViewModeKey, value);
        }
        
        private const string AutoBackupEnabledKey = "pref_autoBackupEnabled";
        private const bool AutoBackupEnabledDefault = false;
        public bool AutoBackupEnabled
        {
            get => _preferences.GetBoolean(AutoBackupEnabledKey, AutoBackupEnabledDefault);
            set => SetPreference(AutoBackupEnabledKey, value);
        }
        
        private const string AutoRestoreEnabledKey = "pref_autoRestoreEnabled";
        private const bool AutoRestoreEnabledDefault = false;
        public bool AutoRestoreEnabled
        {
            get => _preferences.GetBoolean(AutoRestoreEnabledKey, AutoRestoreEnabledDefault);
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
        #endregion
        
        #region State
        private const string FirstLaunchKey = "firstLaunch";
        private const bool FirstLaunchDefault = true;
        public bool FirstLaunch 
        {
            get => _preferences.GetBoolean(FirstLaunchKey, FirstLaunchDefault);
            set => SetPreference(FirstLaunchKey, value);
        }

        private const string DefaultCategoryKey = "defaultCategory";
        private const string DefaultCategoryDefault = null;
        public string DefaultCategory
        {
            get => _preferences.GetString(DefaultCategoryKey, DefaultCategoryDefault);
            set => SetPreference(DefaultCategoryKey, value);
        }

        private const string PasswordChangedKey = "passwordChanged";
        private const bool PasswordChangedDefault = false;
        public bool PasswordChanged 
        {
            get => _preferences.GetBoolean(PasswordChangedKey, PasswordChangedDefault);
            set => SetPreference(PasswordChangedKey, value);
        }

        private const string AutoRestoreCompletedKey = "autoRestoreCompleted";
        private const bool AutoRestoreCompletedDefault = false;
        public bool AutoRestoreCompleted
        {
            get => _preferences.GetBoolean(AutoRestoreCompletedKey, AutoRestoreCompletedDefault);
            set => SetPreference(AutoRestoreCompletedKey, value);
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
            get => _preferences.GetBoolean(AutoBackupTriggerKey, AutoBackupTriggerDefault);
            set => SetPreference(AutoBackupTriggerKey, value);
        }
        
        private const string AutoRestoreTriggerKey = "autoRestoreTrigger";
        private const bool AutoRestoreTriggerDefault = false;
        public bool AutoRestoreTrigger
        {
            get => _preferences.GetBoolean(AutoRestoreTriggerKey, AutoRestoreTriggerDefault);
            set => SetPreference(AutoRestoreTriggerKey, value);
        }
        
        private const string MostRecentBackupModifiedAtKey = "mostRecentBackupModifiedAt";
        private const long MostRecentBackupModifiedAtDefault = 0;
        public long MostRecentBackupModifiedAt
        {
            get => _preferences.GetLong(MostRecentBackupModifiedAtKey, MostRecentBackupModifiedAtDefault);
            set => SetPreference(MostRecentBackupModifiedAtKey, value);
        }
        #endregion
        
        private readonly ISharedPreferences _preferences;
        
        public PreferenceWrapper(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }
        
        private T GetEnumPreference<T>(string key, T defaultValue) where T : Enum
        {
            return (T) (object) _preferences.GetInt(key, (int) (object) defaultValue);
        }
        
        private void SetEnumPreference<T>(string key, T value) where T : Enum
        {
            _preferences.Edit().PutInt(key, (int) (object) value).Commit();
        }
        
        private bool? GetNullableBooleanPreference(string key, bool? defaultValue)
        {
            var defaultStr = defaultValue switch
            {
                null => null,
                false => "false",
                true => "true"
            };
            
            return _preferences.GetString(key, defaultStr) switch
            {
                null => null,
                "false" => false,
                _ => true,
            };
        }
        
        private void SetNullableBooleanPreference(string key, bool? value)
        {
            _preferences.Edit().PutString(key, value switch
            {
                null => null,
                false => "false",
                true => "true"
            }).Commit();
        }

        private Uri GetUriPreference(string key, Uri defaultValue)
        {
            var value = _preferences.GetString(key, null);

            return value == null
                ? defaultValue
                : Uri.Parse(value);
        }

        private void SetUriPreference(string key, Uri value)
        {
            SetPreference(key, value?.ToString());
        }

        private void SetPreference(string key, string value)
        {
            _preferences.Edit().PutString(key, value).Commit();
        }
        
        private void SetPreference(string key, bool value)
        {
            _preferences.Edit().PutBoolean(key, value).Commit();
        }
        
        private void SetPreference(string key, long value)
        {
            _preferences.Edit().PutLong(key, value).Commit();
        }
    }
}