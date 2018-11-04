using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Preference = Android.Support.V7.Preferences.Preference;

namespace PlusAuth.Preferences
{
    internal class RatePreference : Preference
    {
        public RatePreference(Context context) : base(context)
        {

        }

        public RatePreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public RatePreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public RatePreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected RatePreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override void OnClick()
        {
            Intent intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=me.jmh.plusauth"));
            Context.StartActivity(intent);
        }
    }
}