using System;
using Android.Content;
using AndroidX.Preference;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Shared.Util
{
    public abstract class BasePreferenceWrapper
    {
        protected readonly ISharedPreferences _preferences;
        
        protected BasePreferenceWrapper(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }
        
        protected T GetEnumPreference<T>(string key, T defaultValue) where T : Enum
        {
            return (T) (object) _preferences.GetInt(key, (int) (object) defaultValue);
        }
        
        protected void SetEnumPreference<T>(string key, T value) where T : Enum
        {
            _preferences.Edit().PutInt(key, (int) (object) value).Commit();
        }
        
        protected bool? GetNullableBooleanPreference(string key, bool? defaultValue)
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
                _ => true
            };
        }
        
        protected void SetNullableBooleanPreference(string key, bool? value)
        {
            _preferences.Edit().PutString(key, value switch
            {
                null => null,
                false => "false",
                true => "true"
            }).Commit();
        }
        
        protected int? GetNullableIntPreference(string key, int? defaultValue)
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
        
        protected void SetNullableIntPreference(string key, int? value)
        {
            _preferences.Edit().PutString(key, value switch
            {
                null => null,
                _ => value.ToString()
            }).Commit();
        }

        protected Uri GetUriPreference(string key, Uri defaultValue)
        {
            var value = _preferences.GetString(key, null);

            return value == null
                ? defaultValue
                : Uri.Parse(value);
        }

        protected void SetUriPreference(string key, Uri value)
        {
            SetPreference(key, value?.ToString());
        }

        protected void SetPreference(string key, string value)
        {
            _preferences.Edit().PutString(key, value).Commit();
        }
        
        protected void SetPreference(string key, bool value)
        {
            _preferences.Edit().PutBoolean(key, value).Commit();
        }
        
        protected void SetPreference(string key, long value)
        {
            _preferences.Edit().PutLong(key, value).Commit();
        }
    }
}