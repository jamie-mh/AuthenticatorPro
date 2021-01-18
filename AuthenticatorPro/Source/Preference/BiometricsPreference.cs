using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.Preference;
using AuthenticatorPro.Activity;

namespace AuthenticatorPro.Preference
{
    internal class BiometricsPreference : SwitchPreference
    {
        public BiometricsPreference(Context context) : base(context)
        {

        }

        public BiometricsPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {

        }

        public BiometricsPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {

        }

        public BiometricsPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context,
            attrs, defStyleAttr, defStyleRes)
        {

        }

        protected BiometricsPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {

        }

        protected override void OnClick()
        {
            var activity = (SettingsActivity) Context;
            
            try
            {
                if(Checked)
                {
                    activity.ClearBiometrics();
                    base.OnClick();
                }
                else
                {
                    activity.EnableBiometrics(success =>
                    {
                        if(success)
                            base.OnClick();
                    });
                }
            }
            catch
            {
                Toast.MakeText(activity, Resource.String.genericError, ToastLength.Short).Show();
            }
        }
    }
}