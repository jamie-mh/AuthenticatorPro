using Android.Content.Res;
using Android.OS;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Droid.Util;

namespace AuthenticatorPro.Droid.Activity
{
    internal abstract class BaseActivity : AppCompatActivity
    {
        public BaseApplication BaseApplication { get; private set; }
        protected bool IsDark { get; private set; }

        private bool _checkedOnCreate;
        private string _lastTheme;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            _checkedOnCreate = true;
            UpdateTheme();
            base.OnCreate(savedInstanceState);

            BaseApplication = (BaseApplication) Application;

            if(Build.VERSION.SdkInt < BuildVersionCodes.M)
                Window.SetStatusBarColor(Android.Graphics.Color.Black);
        }

        protected void UpdateTheme()
        {
            var preferences = new PreferenceWrapper(this);
            var theme = preferences.Theme;
            
            if(theme == _lastTheme)
                return; 
            
            switch(theme)
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

            _lastTheme = theme;
        }

        protected override void OnResume()
        {
            base.OnResume();
            
            if(_checkedOnCreate)
            {
                _checkedOnCreate = false;
                return;
            }
            
            UpdateTheme();
        }
    }
}