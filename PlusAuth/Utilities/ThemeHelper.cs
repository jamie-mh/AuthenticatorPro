using Android.App;
using Android.Content;
using Android.Support.V7.Preferences;

namespace PlusAuth.Utilities
{
    internal static class ThemeHelper
    {
        public static void Update(Activity activity)
        {
            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(activity);
            bool useDarkTheme = sharedPrefs.GetBoolean("pref_useDarkTheme", false);
            activity.SetTheme(useDarkTheme ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }
    }
}