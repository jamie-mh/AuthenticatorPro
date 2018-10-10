using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace ProAuth.Preferences
{
    public class PasswordPreference : DialogPreference
    {
        public PasswordPreference(Context context) : base(context)
        {
        }
    }
}