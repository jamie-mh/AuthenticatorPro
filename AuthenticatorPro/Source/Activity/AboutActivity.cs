using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Google.Android.Material.AppBar;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class AboutActivity : DayNightActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityAbout);

            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.about);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            string version;

            try
            {
                var packageInfo = PackageManager.GetPackageInfo(PackageName!, 0);
                version = packageInfo.VersionName;
            }
            catch
            {
                version = "unknown";
            }

            var webView = FindViewById<WebView>(Resource.Id.webView);
            webView.Settings.JavaScriptEnabled = true;
            webView.LoadUrl($"file:///android_asset/about.html#{version}");
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch(item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}