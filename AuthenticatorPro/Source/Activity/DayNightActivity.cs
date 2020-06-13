using Android.Content.Res;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace AuthenticatorPro.Activity
{
    internal abstract class DayNightActivity : AppCompatActivity
    {
        protected bool IsDark { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var themePref = sharedPrefs.GetString("pref_theme", "0");

            switch(themePref)
            {
                case "0":
                    IsDark = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightFollowSystem;
                    break;

                case "1":
                    IsDark = false;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
                    break;

                case "2":
                    IsDark = true;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
                    break;
            }
        }
    }
}