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
        
        private readonly ISharedPreferences _preferences;
        
        public PreferenceWrapper(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }
        private void SetPreference(string key, string value)
        {
            _preferences.Edit().PutString(key, value).Commit();
        }
    }
}