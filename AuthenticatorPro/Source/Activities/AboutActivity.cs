using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AndroidX.AppCompat.Widget;

namespace AuthenticatorPro.Activities
{
    [Activity]
    internal class AboutActivity : LightDarkActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityAbout);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityAbout_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.about);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            var webView = FindViewById<WebView>(Resource.Id.activityAbout_webView);
            webView.LoadUrl(@"file:///android_asset/about.html");
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