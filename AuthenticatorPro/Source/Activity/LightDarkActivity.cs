using Android.Content.Res;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Preference;

namespace AuthenticatorPro.Activity
{
    internal abstract class LightDarkActivity : AppCompatActivity
    {
        private string _currThemePref;
        protected bool IsDark { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetNightMode(GetThemePreference());
        }

        private string GetThemePreference()
        {
            var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            return sharedPrefs.GetString("pref_theme", "0");
        }

        private void SetNightMode(string themePref)
        {
            _currThemePref = themePref;

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

        protected override void OnResume()
        {
            base.OnResume();
            var themePref = GetThemePreference();

            if(_currThemePref != themePref)
                Recreate();

            SetNightMode(themePref);
        }
    }
}