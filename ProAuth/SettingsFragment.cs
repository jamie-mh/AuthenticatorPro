using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;

namespace ProAuth
{
    public class SettingsFragment : PreferenceFragmentCompat
    {
        public override void OnCreatePreferences(Bundle savedInstanceState, string rootKey) 
        {
            AddPreferencesFromResource(Resource.Xml.settings);
        }
    }
}