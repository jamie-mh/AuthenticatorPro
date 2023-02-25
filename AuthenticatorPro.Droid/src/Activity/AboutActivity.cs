// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.AppBar;
using System;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class AboutActivity : BaseActivity
    {
        public AboutActivity() : base(Resource.Layout.activityAbout) { }

        protected override void OnCreate(Bundle savedInstanceState)
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

            var webView = FindViewById<WebView>(Resource.Id.webView);
            webView.Settings.JavaScriptEnabled = true;
            webView.LoadUrl($"file:///android_asset/about.html#{version}|{(IsDark ? "dark" : "light")}");
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