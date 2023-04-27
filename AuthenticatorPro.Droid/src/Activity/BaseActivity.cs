// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using AndroidX.AppCompat.App;
using AndroidX.Core.Content;
using AuthenticatorPro.Droid.Interface;
using Google.Android.Material.Color;
using Java.Util;

namespace AuthenticatorPro.Droid.Activity
{
    internal abstract class BaseActivity : AppCompatActivity
    {
        public BaseApplication BaseApplication { get; private set; }
        public bool IsDark { get; private set; }

        private readonly int _layout;
        private PreferenceWrapper _preferences;

        private bool _updatedThemeOnCreate;
        private string _lastTheme;

        protected BaseActivity(int layout)
        {
            _layout = layout;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _updatedThemeOnCreate = true;
            UpdateTheme();

            BaseApplication = (BaseApplication) Application;

            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                Window.SetStatusBarColor(Color.Black);
            }

            var overlay = AccentColourMap.GetOverlayId(_preferences.AccentColour);
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var dynamicColorOptions = new DynamicColorsOptions.Builder();

                if (!_preferences.DynamicColour)
                {
                    dynamicColorOptions.SetThemeOverlay(overlay);
                }

                DynamicColors.ApplyToActivityIfAvailable(this, dynamicColorOptions.Build());
            }
            else
            {
                Theme.ApplyStyle(overlay, true);
            }

            SetContentView(_layout);
        }

        protected override void AttachBaseContext(Context context)
        {
            _preferences = new PreferenceWrapper(context);
            var language = _preferences.Language;

            var resources = context.Resources;
            var config = resources?.Configuration;

            Locale locale;

            if (language == "system")
            {
                locale = Locale.Default;
            }
            else if (language.Contains('-'))
            {
                var parts = language.Split('-', 2);
                locale = new Locale(parts[0], parts[1]);
            }
            else
            {
                locale = new Locale(language);
            }

            config?.SetLocale(locale);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                if (config != null)
                {
                    context = context.CreateConfigurationContext(config);
                }
            }
            else
            {
#pragma warning disable CA1422
                resources?.UpdateConfiguration(config, resources.DisplayMetrics);
#pragma warning restore CA1422
            }

            base.AttachBaseContext(context);
        }

        protected void UpdateTheme()
        {
            var theme = _preferences.Theme;

            if (theme == _lastTheme)
            {
                return;
            }

            switch (theme)
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

            var label = GetString(Resource.String.appName);
            var colourId = AccentColourMap.GetColourId(_preferences.AccentColour);
            var colour = new Color(ContextCompat.GetColor(this, colourId));

            switch (Build.VERSION.SdkInt)
            {
                case >= BuildVersionCodes.Tiramisu:
                {
#pragma warning disable CA1416
                    var description = new ActivityManager.TaskDescription.Builder()
                        .SetLabel(label)
                        .SetIcon(Resource.Mipmap.ic_launcher);

                    if (!_preferences.DynamicColour)
                    {
                        description = description.SetPrimaryColor(colour);
                    }

                    SetTaskDescription(description.Build());
                    break;
#pragma warning restore CA1416
                }

                case >= BuildVersionCodes.P:
#pragma warning disable CS0618
#pragma warning disable CA1416
                    SetTaskDescription(new ActivityManager.TaskDescription(label, Resource.Mipmap.ic_launcher, colour));
#pragma warning restore CA1416
#pragma warning restore CS0618
                    break;

                default:
                {
                    var bitmap = BitmapFactory.DecodeResource(Resources, Resource.Mipmap.ic_launcher);
#pragma warning disable CS0618
                    SetTaskDescription(new ActivityManager.TaskDescription(label, bitmap, colour));
#pragma warning restore CS0618
                    break;
                }
            }

            if (_updatedThemeOnCreate)
            {
                _updatedThemeOnCreate = false;
                return;
            }

            UpdateTheme();
        }
    }
}