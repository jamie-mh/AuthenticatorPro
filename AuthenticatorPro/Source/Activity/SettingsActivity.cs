using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Preference;
using AuthenticatorPro.Data;
using AuthenticatorPro.Fragment;
using Task = System.Threading.Tasks.Task;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class SettingsActivity : DayNightActivity, ISharedPreferencesOnSharedPreferenceChangeListener
    {
        private SettingsFragment _fragment;
        private ProgressBar _progressBar;
        private bool _shouldRecreateMain;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // If a setting that requires changes to the main activity has changed
            // return a result telling it to recreate.
            _shouldRecreateMain = savedInstanceState != null && savedInstanceState.GetBoolean("shouldRecreateMain", false);

            SetContentView(Resource.Layout.activitySettings);
            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.settings);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);

            var prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            prefs.RegisterOnSharedPreferenceChangeListener(this);

            _fragment = new SettingsFragment();
            _fragment.PreferencesCreated += delegate
            {
                UpdateBackupRemindersEnabled(prefs);
            };

            SupportFragmentManager.BeginTransaction()
                .Replace(Resource.Id.layoutFragment, _fragment)
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
            switch(key)
            {
                case "pref_useEncryptedDatabase":
                case "pref_viewMode":
                    _shouldRecreateMain = true;
                    break;
                
                case "pref_theme":
                    UpdateTheme();
                    break;
                
                case "pref_autoBackupEnabled":
                    UpdateBackupRemindersEnabled(sharedPreferences);
                    break;
            }
        }

        public async Task SetDatabaseEncryption(bool shouldEncrypt)
        {
            RunOnUiThread(delegate
            {
                _progressBar.Visibility = ViewStates.Visible;
                Window.SetFlags(WindowManagerFlags.NotTouchable, WindowManagerFlags.NotTouchable);
            });

            try
            {
                await Database.SetEncryptionEnabled(this, shouldEncrypt);
            }
            finally
            {
                RunOnUiThread(delegate
                {
                    Window.ClearFlags(WindowManagerFlags.NotTouchable);
                    _progressBar.Visibility = ViewStates.Invisible;
                });
            }
        }

        private void UpdateBackupRemindersEnabled(ISharedPreferences sharedPreferences)
        {
            var autoBackupEnabled = sharedPreferences.GetBoolean("pref_autoBackupEnabled", false);
            _fragment.FindPreference("pref_showBackupReminders").Enabled = !autoBackupEnabled;
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

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            outState.PutBoolean("shouldRecreateMain", _shouldRecreateMain);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}