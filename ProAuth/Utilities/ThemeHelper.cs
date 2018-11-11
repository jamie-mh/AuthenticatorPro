using Android.App;
using Android.Content;
using Android.Support.V7.Preferences;

namespace ProAuth.Utilities
{
    internal static class ThemeHelper
    {
        public static bool Checked = false;
        public static bool IsDark = false;

        public static void Update(Activity activity)
        {
            if(!Checked)
            {
                ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(activity);
                IsDark = sharedPrefs.GetBoolean("pref_useDarkTheme", false);
                Checked = true;
            }

            activity.SetTheme(IsDark ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }
    }
}