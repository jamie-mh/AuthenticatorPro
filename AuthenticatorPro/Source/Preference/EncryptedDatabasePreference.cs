using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
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

        protected override async void OnClick()
        {
            // Call the database encryption change here rather than listening for changes.
            // The listener is called post update.
            var activity = (SettingsActivity) Context;

            try
            {
                await activity.SetDatabaseEncryption(!Checked);
                base.OnClick();
            }
            catch
            {
                Toast.MakeText(Context, Resource.String.genericError, ToastLength.Short).Show();
            }
        }
    }
}