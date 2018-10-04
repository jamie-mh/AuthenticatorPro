using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace ProAuth
{
    [Activity(Label = "AboutActivity")]
    public class AboutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityAbout);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityAbout_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.about);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            WebView webView = FindViewById<WebView>(Resource.Id.activityAbout_webView);
            webView.LoadUrl(@"file:///android_asset/about.html");
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
            return base.OnOptionsItemSelected (item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}