// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.Color;
using System;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class AboutActivity : BaseActivity
    {
        public AboutActivity() : base(Resource.Layout.activityAbout) { }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

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
                Logger.Error(e);
                version = "unknown";
            }
            
            var surface = MaterialColors.GetColor(this, Resource.Attribute.colorSurface, 0);
            var onSurface = MaterialColors.GetColor(this, Resource.Attribute.colorOnSurface, 0);
            var primary = MaterialColors.GetColor(this, Resource.Attribute.colorPrimary, 0);

            var icon = await AssetUtil.ReadAllBytes(Assets, "icon.png");
            
            var html = (await AssetUtil.ReadAllTextAsync(Assets, "about.html"))
                .Replace("%ICON", $"data:image/png;base64,{Convert.ToBase64String(icon)}")
                .Replace("%VERSION", version)
                .Replace("%SURFACE", ColourToHexString(surface))
                .Replace("%ON_SURFACE", ColourToHexString(onSurface))
                .Replace("%PRIMARY", ColourToHexString(primary));
            
            var webView = FindViewById<WebView>(Resource.Id.webView);
            webView.LoadDataWithBaseURL("file:///android_asset", html, "text/html", "utf-8", null);
        }

        private static string ColourToHexString(int colour)
        {
            var parsed = new Color(colour);
            return "#" + parsed.R.ToString("X2") + parsed.G.ToString("X2") + parsed.B.ToString("X2");
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }
    }
}