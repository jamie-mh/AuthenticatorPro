using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Preference;
using AuthenticatorPro.Activity;

namespace AuthenticatorPro.Preference
{
    internal class EncryptedDatabasePreference : SwitchPreferenceCompat
    {
        public EncryptedDatabasePreference(Context context) : base(context)
        {

        }

        public EncryptedDatabasePreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public EncryptedDatabasePreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {

        }

        public EncryptedDatabasePreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context,
            attrs, defStyleAttr, defStyleRes)
        {

        }

        protected EncryptedDatabasePreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        protected override void OnClick()
        {
            var activity = (SettingsActivity) Context;
            activity.SetDatabaseEncryption(!Checked);
            base.OnClick();
        }
    }
}