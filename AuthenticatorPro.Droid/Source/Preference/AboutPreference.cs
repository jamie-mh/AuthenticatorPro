using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AuthenticatorPro.Droid.Activity;

namespace AuthenticatorPro.Droid.Preference
{
    internal class AboutPreference : AndroidX.Preference.Preference
    {
        public AboutPreference(Context context) : base(context)
        {

        }

        public AboutPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public AboutPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {

        }

        public AboutPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context,
            attrs, defStyleAttr, defStyleRes)
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