using Android.Content.Res;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace AuthenticatorPro.Activity
{
    internal abstract class DayNightActivity : AppCompatActivity
    {
        protected bool IsDark { get; private set; }

        protected override void OnResume()
        {
            base.OnResume();
            
            var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var themePref = sharedPrefs.GetString("pref_theme", "system");

            switch(themePref)
            {
                default:
                    IsDark = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightFollowSystem;
                    break;

                case "light":
                    IsDark = false;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightNo;
                    break;

                case "dark":
                    IsDark = true;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
                    break;
            }
        }
    }
}