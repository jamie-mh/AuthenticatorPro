using Android.App;
using AndroidX.Preference;

namespace AuthenticatorPro
{
    internal static class Theme
    {
        public static bool Checked { get; private set; }
        public static bool IsDark { get; private set; }

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