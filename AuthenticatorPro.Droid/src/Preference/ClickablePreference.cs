// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Runtime;
using Android.Util;
using System;

namespace AuthenticatorPro.Droid.Preference
{
    public class ClickablePreference : AndroidX.Preference.Preference
    {
        public event EventHandler Clicked;
        
        protected ClickablePreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public ClickablePreference(Context context) : base(context)
        {
        }

        public ClickablePreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ClickablePreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        public ClickablePreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }
        
        protected override void OnClick()
        {
            Clicked?.Invoke(this, EventArgs.Empty);
        }
    }
}