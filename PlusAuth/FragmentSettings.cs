using Android.OS;
using Android.Support.V7.Preferences;

namespace PlusAuth
{
    public class FragmentSettings : PreferenceFragmentCompat
    {
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey)
        {
            AddPreferencesFromResource(Resource.Xml.settings);
        }
    }
}