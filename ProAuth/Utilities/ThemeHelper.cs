using Android.App;
using Android.Content;
using Android.Support.V7.Preferences;

namespace ProAuth.Utilities
{
    internal static class ThemeHelper
    {
        public static bool IsDark;

        public static void Update(Activity activity)
        {
            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(activity);
            IsDark = sharedPrefs.GetBoolean("pref_useDarkTheme", false);
            activity.SetTheme(IsDark ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }
    }
}