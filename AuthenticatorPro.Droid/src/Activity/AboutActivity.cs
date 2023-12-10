// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AndroidX.Core.Widget;
using AuthenticatorPro.Core;
using Android.Widget;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Color;
using Insets = AndroidX.Core.Graphics.Insets;
using Serilog;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Environment = System.Environment;
using Path = System.IO.Path;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    public class AboutActivity : BaseActivity
    {
        private readonly ILogger _log = Log.ForContext<AboutActivity>();
        private readonly IAssetProvider _assetProvider;
        private WebView _webView;

        public AboutActivity() : base(Resource.Layout.activityAbout)
        {
            _assetProvider = Dependencies.Resolve<IAssetProvider>();
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SupportActionBar.SetTitle(Resource.String.about);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            string version;

            try
            {
#if FDROID
                var versionName = PackageUtil.GetVersionName(PackageManager, PackageName);
                version = $"{versionName} F-Droid";
#else
                version = PackageUtil.GetVersionName(PackageManager, PackageName);
#endif
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to get current version");
                version = "unknown";
            }

            var surface = MaterialColors.GetColor(this, Resource.Attribute.colorSurface, 0);
            var onSurface = MaterialColors.GetColor(this, Resource.Attribute.colorOnSurface, 0);
            var primary = MaterialColors.GetColor(this, Resource.Attribute.colorPrimary, 0);

            var icon = await _assetProvider.ReadBytesAsync("icon.png");

            var html = (await _assetProvider.ReadStringAsync("about.html"))
                .Replace("%ICON", $"data:image/png;base64,{Convert.ToBase64String(icon)}")
                .Replace("%VERSION", version)
                .Replace("%SURFACE", ColourToHexString(surface))
                .Replace("%ON_SURFACE", ColourToHexString(onSurface))
                .Replace("%PRIMARY", ColourToHexString(primary));

#if !FDROID
            var extraLicense = await _assetProvider.ReadStringAsync("license.extra.html");
            html = html.Replace("%LICENSE", extraLicense);
#endif

            _webView = FindViewById<WebView>(Resource.Id.webView);
            _webView.LoadDataWithBaseURL("file:///android_asset", html, "text/html", "utf-8", null);
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.about, menu);
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;

                case Resource.Id.actionViewLog:
                    ShowDebugLog();
                    return true;

                default:
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            base.OnApplySystemBarInsets(insets);
            var scrollView = FindViewById<NestedScrollView>(Resource.Id.nestedScrollView);
            scrollView.SetPadding(0, 0, 0, insets.Bottom);
        }

        private void ShowDebugLog()
        {
            var path = GetLogPath();

            if (path == null)
            {
                Toast.MakeText(this, Resource.String.noLogFile, ToastLength.Short).Show();
                return;
            }
            
            Toolbar.SetTitle(Resource.String.debugLog);
            Toolbar.Menu.RemoveItem(Resource.Id.actionViewLog);
            
            Task.Run(async delegate
            {
                var data = await File.ReadAllTextAsync(path);
                RunOnUiThread(delegate { _webView.LoadData(data, "text/plain", "utf-8"); });
            });
        }

        private static string GetLogPath()
        {
            var privateDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var file = Directory.GetFiles(privateDir, "*.log").FirstOrDefault();
            return file == null ? null : Path.Combine(privateDir, file);
        }

        private static string ColourToHexString(int colour)
        {
            var parsed = new Color(colour);
            return "#" + parsed.R.ToString("X2") + parsed.G.ToString("X2") + parsed.B.ToString("X2");
        }
    }
}