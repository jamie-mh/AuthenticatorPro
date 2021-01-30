using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.AppBar;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ErrorActivity : BaseActivity
    {
        private string _exception;
    
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

            _exception = Intent.GetStringExtra("exception");
            var textError = FindViewById<TextView>(Resource.Id.errorText);
            textError.Text = _exception;
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            MenuInflater.Inflate(Resource.Menu.error, menu);
            return base.OnCreateOptionsMenu(menu);
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
                
                case Resource.Id.actionReport:
                    ReportError();
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ReportError()
        {
            var clipboard = (ClipboardManager) GetSystemService(ClipboardService);
            var clip = ClipData.NewPlainText("error", _exception);
            clipboard.PrimaryClip = clip;
            
            Toast.MakeText(this, Resource.String.errorCopiedToClipboard, ToastLength.Short).Show();
            
            var intent = new Intent(Intent.ActionView, Uri.Parse($"{Constants.GitHubRepo}/issues"));
            
            try
            {
                StartActivity(intent);
            }
            catch(ActivityNotFoundException)
            {
                Toast.MakeText(this, Resource.String.webBrowserMissing, ToastLength.Short).Show(); 
            }
        }
    }
}