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
using Preference = Android.Support.V7.Preferences.Preference;

namespace PlusAuth.Preferences
{
    internal class AboutPreference : Preference
    {
        public AboutPreference(Context context) : base(context)
        {

        }

        public AboutPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public AboutPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public AboutPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected AboutPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override void OnClick()
        {
            Context.StartActivity(typeof(AboutActivity));
        }
    }
}