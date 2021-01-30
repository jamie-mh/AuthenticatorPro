using Android.App;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Google.Android.Material.AppBar;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ErrorActivity : BaseActivity 
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityError);
            
            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetTitle(Resource.String.error);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            var exception = Intent.GetStringExtra("exception");
            var webView = FindViewById<WebView>(Resource.Id.webView);
            webView.LoadData($"<pre>{exception}</pre>", "text/html", "utf8");
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
    }
}