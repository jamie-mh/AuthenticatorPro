using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Preference;
using AuthenticatorPro.Fragment;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class SettingsActivity : DayNightActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private ProgressBar _progressBar;
        private bool _shouldRecreateMain;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // If a setting that requires changes to the main activity has changed
            // return a result telling it to recreate.
            _shouldRecreateMain = false;

            SetContentView(Resource.Layout.activitySettings);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.settings);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            PreferenceManager.GetDefaultSharedPreferences(this)
                .RegisterOnSharedPreferenceChangeListener(this);

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.layoutFragment, new SettingsFragment())
                .Commit();
        }

        public override void Finish()
        {
            if(_shouldRecreateMain)
                SetResult(Result.Ok, null);

            base.Finish();
        }

        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            if(key == "pref_useEncryptedDatabase" || key == "pref_theme" || key == "pref_viewMode")
                _shouldRecreateMain = true;
        }

        public async void SetDatabaseEncryption(bool shouldEncrypt)
        {
            _progressBar.Visibility = ViewStates.Visible;
            Window.SetFlags(WindowManagerFlags.NotTouchable, WindowManagerFlags.NotTouchable);

            await Database.SetEncryptionEnabled(this, shouldEncrypt);

            Window.ClearFlags(WindowManagerFlags.NotTouchable);
            _progressBar.Visibility = ViewStates.Invisible;
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