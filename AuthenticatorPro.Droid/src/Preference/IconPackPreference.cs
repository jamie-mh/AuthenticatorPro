// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Runtime;
using Android.Util;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Interface.Fragment;
using System;

namespace AuthenticatorPro.Droid.Preference
{
    internal class IconPackPreference : AndroidX.Preference.Preference
    {
        public IconPackPreference(Context context) : base(context) { }

        public IconPackPreference(Context context, IAttributeSet attrs) : base(context, attrs) { }

        public IconPackPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {
        }

        public IconPackPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(
            context,
            attrs, defStyleAttr, defStyleRes)
        {
        }

        protected IconPackPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        protected override void OnClick()
        {
            var activity = (SettingsActivity) Context;
            var fragment = new IconPackSetupBottomSheet();
            fragment.Show(activity.SupportFragmentManager, fragment.Tag);
        }
    }
}