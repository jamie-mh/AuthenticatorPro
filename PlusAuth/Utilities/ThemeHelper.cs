using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Preferences;
using Android.Views;
using Android.Widget;

namespace PlusAuth.Utilities
{
    internal static class ThemeHelper
    {
        public static void Update(Activity activity)
        {
            ISharedPreferences sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(activity);
            bool useDarkTheme = sharedPrefs.GetBoolean("pref_useDarkTheme", false);
            activity.SetTheme(useDarkTheme ? Resource.Style.DarkTheme : Resource.Style.LightTheme);
        }
    }
}