using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace AuthenticatorPro.Activities
{
    internal abstract class LightDarkActivity : AppCompatActivity
    {
        protected bool IsDark { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            IsDark = GetDarkPreference();
            SetTheme(IsDark ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }

        private bool GetDarkPreference()
        {
            var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            return sharedPrefs.GetBoolean("pref_useDarkTheme", false);
        }

        protected override void OnResume()
        {
            base.OnResume();
            var isDarkPref = GetDarkPreference();

            if(IsDark != isDarkPref)
                Recreate();

            IsDark = isDarkPref;
        }
    }
}