using Android.App;
using Android.Support.V7.Preferences;

namespace AuthenticatorPro.Utilities
{
    internal static class ThemeHelper
    {
        public static bool Checked;
        public static bool IsDark;

        public static void Update(Activity activity)
        {
            if(!Checked)
            {
                var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(activity);
                IsDark = sharedPrefs.GetBoolean("pref_useDarkTheme", false);
                Checked = true;
            }

            activity.SetTheme(IsDark ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }
    }
}