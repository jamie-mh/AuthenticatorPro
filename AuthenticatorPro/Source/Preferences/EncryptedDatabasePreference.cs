using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Preference;

namespace AuthenticatorPro.Preferences
{
    internal class EncryptedDatabasePreference : CheckBoxPreference
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
            await Database.UpdateEncryption(Context, !Checked);
            base.OnClick();
        }
    }
}