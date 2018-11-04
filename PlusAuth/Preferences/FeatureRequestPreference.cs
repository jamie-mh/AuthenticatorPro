using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Preference = Android.Support.V7.Preferences.Preference;
using Uri = Android.Net.Uri;

namespace PlusAuth.Preferences
{
    internal class FeatureRequestPreference : Preference
    {
        public FeatureRequestPreference(Context context) : base(context)
        {

        }

        public FeatureRequestPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public FeatureRequestPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public FeatureRequestPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected FeatureRequestPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected override void OnClick()
        {
            Uri uri = Uri.FromParts("mailto", "support@jmh.me", null);
            Intent intent = new Intent(Intent.ActionSendto, uri);
            intent.PutExtra(Intent.ExtraSubject, Context.GetString(Resource.String.pref_featureRequest_title));

            Context.StartActivity(Intent.CreateChooser(intent, Context.GetString(Resource.String.sendEmail)));
        }
    }
}