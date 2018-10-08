using Android.OS;
using Android.Preferences;

namespace ProAuth
{
    public class SettingsFragment : PreferenceFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AddPreferencesFromResource(Resource.Xml.settings);
        }
    }
}