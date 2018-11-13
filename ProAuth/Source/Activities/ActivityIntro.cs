using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Preferences;
using Android.Widget;

namespace ProAuth.Activities
{
    [Activity(Label = "IntroActivity", Theme = "@style/LightTheme")]
    public class ActivityIntro : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityIntro);

            Button continueButton = FindViewById<Button>(Resource.Id.activityIntro_continue);
            continueButton.Click += (sender, e) => { Close(); };
        }

        private void Close()
        {
            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = sharedPrefs.Edit();

            editor.PutBoolean("firstLaunch", false);
            editor.Commit();

            Finish();
        }

        public override void OnBackPressed()
        {
            Close();
            base.OnBackPressed();
        }
    }
}