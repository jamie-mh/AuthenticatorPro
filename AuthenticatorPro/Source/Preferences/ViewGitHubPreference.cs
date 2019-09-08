using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Preference;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Preferences
{
    internal class ViewGitHubPreference : Preference
    {
        public ViewGitHubPreference(Context context) : base(context)
        {
        }

        public ViewGitHubPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ViewGitHubPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {
        }

        public ViewGitHubPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(
            context, attrs, defStyleAttr, defStyleRes)
        {
        }

        protected ViewGitHubPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        protected override void OnClick()
        {
            var intent = new Intent(Intent.ActionView, Uri.Parse("https://www.github.com/jamie-mh/AuthenticatorPro"));
            Context.StartActivity(intent);
        }
    }
}