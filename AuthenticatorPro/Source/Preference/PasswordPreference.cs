using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AuthenticatorPro.Activity;
using AuthenticatorPro.Fragment;

namespace AuthenticatorPro.Preference
{
    internal class PasswordPreference : AndroidX.Preference.Preference
    {
        public PasswordPreference(Context context) : base(context)
        {

        }

        public PasswordPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public PasswordPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {

        }

        public PasswordPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context,
            attrs, defStyleAttr, defStyleRes)
        {

        }

        protected PasswordPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        protected override void OnClick()
        {
            var activity = (SettingsActivity) Context;
            var fragment = new PasswordSetupBottomSheet();
            fragment.Show(activity.SupportFragmentManager, fragment.Tag);
        }
    }
}