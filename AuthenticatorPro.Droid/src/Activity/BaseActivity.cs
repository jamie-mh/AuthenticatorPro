// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.Content;
using AndroidX.Core.View;
using AuthenticatorPro.Droid.Interface;
using Google.Android.Material.AppBar;
using Google.Android.Material.Color;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Java.Util;
using Insets = AndroidX.Core.Graphics.Insets;

namespace AuthenticatorPro.Droid.Activity
{
    public abstract class BaseActivity : AppCompatActivity, IOnApplyWindowInsetsListener
    {
        protected const int ListFabPaddingBottom = 74;

        // Internal state
        private readonly int _layout;
        private bool _updatedThemeOnCreate;
        private bool _appliedSystemBarInsets;
        private string _lastTheme;

        // Common data
        protected PreferenceWrapper Preferences;

        // Common interface elements
        protected CoordinatorLayout RootLayout;
        protected LinearLayout ToolbarWrapLayout;
        protected AppBarLayout AppBarLayout;
        protected MaterialToolbar Toolbar;
        protected LinearProgressIndicator ProgressIndicator;
        protected FloatingActionButton AddButton;

        protected BaseActivity(int layout)
        {
            _layout = layout;
        }

        public BaseApplication BaseApplication { get; private set; }
        public bool IsDark { get; private set; }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            BaseApplication = (BaseApplication) Application;
            _updatedThemeOnCreate = true;

            UpdateTheme();
            UpdateOverlay();
            SetContentView(_layout);
            UpdateStatusBar();

            RootLayout = FindViewById<CoordinatorLayout>(Resource.Id.layoutRoot);
            AppBarLayout = FindViewById<AppBarLayout>(Resource.Id.appBarLayout);
            ToolbarWrapLayout = FindViewById<LinearLayout>(Resource.Id.toolbarWrapLayout);
            ProgressIndicator = FindViewById<LinearProgressIndicator>(Resource.Id.appBarProgressIndicator);
            Toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            AddButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);

            if (RootLayout != null)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(RootLayout, this);
            }

            if (Toolbar != null)
            {
                SetSupportActionBar(Toolbar);
            }
        }

        protected override void AttachBaseContext(Context context)
        {
            Preferences = new PreferenceWrapper(context);
            var language = Preferences.Language;

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
        
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            _appliedSystemBarInsets = false;
        }

        private void UpdateTheme()
        {
            var theme = Preferences.Theme;

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
                case "black":
                    IsDark = true;
                    AppCompatDelegate.DefaultNightMode = AppCompatDelegate.ModeNightYes;
                    break;
            }

            _lastTheme = theme;
        }

        private void UpdateOverlay()
        {
            var overlay = AccentColourMap.GetOverlayId(Preferences.AccentColour);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                var dynamicColorOptions = new DynamicColorsOptions.Builder();

                if (!Preferences.DynamicColour)
                {
                    dynamicColorOptions.SetThemeOverlay(overlay);
                }

                DynamicColors.ApplyToActivityIfAvailable(this, dynamicColorOptions.Build());
            }
            else
            {
                Theme.ApplyStyle(overlay, true);
            }

            if (Preferences.Theme == "black" || Preferences.Theme == "system-black" && IsDark)
            {
                Theme.ApplyStyle(Resource.Style.OverlayBlack, true);
            }
        }

        private void UpdateStatusBar()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.R)
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                {
                    Window.SetStatusBarColor(Color.Black);
                }

                return;
            }

            Window.SetStatusBarColor(Color.Transparent);

#pragma warning disable CA1416
            Window.SetDecorFitsSystemWindows(false);
            Window.SetNavigationBarColor(Color.Transparent);

            if (!IsDark)
            {
                Window.InsetsController?.SetSystemBarsAppearance(
                    (int) WindowInsetsControllerAppearance.LightStatusBars,
                    (int) WindowInsetsControllerAppearance.LightStatusBars);
            }
#pragma warning restore CA1416
        }

        protected override void OnResume()
        {
            base.OnResume();

            var label = GetString(Resource.String.appName);
            var colourId = AccentColourMap.GetColourId(Preferences.AccentColour);
            var colour = new Color(ContextCompat.GetColor(this, colourId));

            switch (Build.VERSION.SdkInt)
            {
                case >= BuildVersionCodes.Tiramisu:
                {
#pragma warning disable CA1416
                    var description = new ActivityManager.TaskDescription.Builder()
                        .SetLabel(label)
                        .SetIcon(Resource.Mipmap.ic_launcher);

                    if (!Preferences.DynamicColour)
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

        public WindowInsetsCompat OnApplyWindowInsets(View view, WindowInsetsCompat insets)
        {
            if (_appliedSystemBarInsets)
            {
                return insets;
            }
            
            var systemBarInsets = insets.GetInsets(WindowInsetsCompat.Type.SystemBars());
            
            var layoutParameters = (ViewGroup.MarginLayoutParams) view.LayoutParameters;
            layoutParameters.LeftMargin = systemBarInsets.Left;
            layoutParameters.RightMargin = systemBarInsets.Right;
            view.LayoutParameters = layoutParameters;
            
            OnApplySystemBarInsets(systemBarInsets);
            _appliedSystemBarInsets = true;
            
            return insets;
        }

        protected virtual void OnApplySystemBarInsets(Insets insets)
        {
            ToolbarWrapLayout?.SetPadding(0, insets.Top, 0, 0);
        }

        #region Common Helpers

        protected void SetLoading(bool loading)
        {
            RunOnUiThread(delegate
            {
                ProgressIndicator.Visibility = loading ? ViewStates.Visible : ViewStates.Invisible;
            });
        }

        protected void ShowSnackbar(int textRes, int length)
        {
            var snackbar = Snackbar.Make(RootLayout, textRes, length);

            if (AddButton != null)
            {
                snackbar.SetAnchorView(AddButton);
            }

            snackbar.Show();
        }

        protected void ShowSnackbar(string message, int length)
        {
            var snackbar = Snackbar.Make(RootLayout, message, length);

            if (AddButton != null)
            {
                snackbar.SetAnchorView(AddButton);
            }

            snackbar.Show();
        }

        protected void StartWebBrowserActivity(string url)
        {
            var intent = new Intent(Intent.ActionView, Uri.Parse(url));

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.webBrowserMissing, Snackbar.LengthLong);
            }
        }

        protected void StartFilePickActivity(string mimeType, int requestCode)
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);

            BaseApplication.PreventNextAutoLock = true;

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong);
            }
        }

        protected void StartFileSaveActivity(string mimeType, int requestCode, string fileName)
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType(mimeType);
            intent.PutExtra(Intent.ExtraTitle, fileName);

            BaseApplication.PreventNextAutoLock = true;

            try
            {
                StartActivityForResult(intent, requestCode);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong);
            }
        }

        #endregion
    }
}