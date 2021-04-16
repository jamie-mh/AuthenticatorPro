using System;
using Android.Content;
using AndroidX.Preference;

namespace AuthenticatorPro.WearOS.Util
{
    internal class PreferenceWrapper
    {
        private const string DefaultCategoryKey = "defaultCategory";
        private const string DefaultCategoryDefault = null;
        public string DefaultCategory
        {
            get => _preferences.GetString(DefaultCategoryKey, DefaultCategoryDefault);
            set => SetPreference(DefaultCategoryKey, value);
        }
        
        private const string DefaultAuthKey = "defaultAuth";
        private static readonly int? DefaultAuthDefault = null;
        public int? DefaultAuth
        {
            get => GetNullableIntPreference(DefaultAuthKey, DefaultAuthDefault);
            set => SetNullableIntPreference(DefaultAuthKey, value);
        }
        
        private readonly ISharedPreferences _preferences;
        
        public PreferenceWrapper(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }
        private void SetPreference(string key, string value)
        {
            _preferences.Edit().PutString(key, value).Commit();
        }
        
        private int? GetNullableIntPreference(string key, int? defaultValue)
        {
            var defaultStr = defaultValue switch
            {
                null => null,
                _ => defaultValue.ToString()
            };

            var result = _preferences.GetString(key, defaultStr);
            
            return result switch {
                null => null,
                _ => Int32.Parse(result)
            };
        }
        
        private void SetNullableIntPreference(string key, int? value)
        {
            _preferences.Edit().PutString(key, value switch
            {
                null => null,
                _ => value.ToString()
            }).Commit();
        }
    }
}