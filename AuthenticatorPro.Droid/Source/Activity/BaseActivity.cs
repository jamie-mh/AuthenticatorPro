// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content.Res;
using Android.OS;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;

namespace AuthenticatorPro.Droid.Activity
{
    internal abstract class BaseActivity : AppCompatActivity
    {
        public BaseApplication BaseApplication { get; private set; }
        public bool IsDark { get; private set; }

        private readonly int _layout;
        private PreferenceWrapper _preferences;
        
        private bool _checkedOnCreate;
        private string _lastTheme;

        protected BaseActivity(int layout)
        {
            _layout = layout;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _preferences = new PreferenceWrapper(this);
            
            _checkedOnCreate = true;
            UpdateTheme();

            BaseApplication = (BaseApplication) Application;

            if(Build.VERSION.SdkInt < BuildVersionCodes.M)
                Window.SetStatusBarColor(Android.Graphics.Color.Black);

            var overlay = AccentColourMap.GetOverlay(_preferences.AccentColour);
            Theme.ApplyStyle(overlay, true);
            SetContentView(_layout);
        }

        protected void UpdateTheme()
        {
            var theme = _preferences.Theme;
            
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