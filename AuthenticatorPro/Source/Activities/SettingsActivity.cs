using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AuthenticatorPro.Fragments;

namespace AuthenticatorPro.Activities
{
    [Activity]
    internal class SettingsActivity : LightDarkActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activitySettings);
            var toolbar = FindViewById<Toolbar>(Resource.Id.activitySettings_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.settings);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back", IsDark));

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.activitySettings_content, new SettingsFragment())
                .Commit();
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
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