// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Preference;

namespace Stratum.Droid.Interface.Preference
{
    public class MaterialSwitchPreference : SwitchPreferenceCompat
    {
        protected MaterialSwitchPreference(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
            WidgetLayoutResource = Resource.Layout.switchPreference;
        }

        public MaterialSwitchPreference(Context context) : base(context)
        {
            WidgetLayoutResource = Resource.Layout.switchPreference;
        }

        public MaterialSwitchPreference(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            WidgetLayoutResource = Resource.Layout.switchPreference;
        }

        public MaterialSwitchPreference(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs,
            defStyleAttr)
        {
            WidgetLayoutResource = Resource.Layout.switchPreference;
        }

        public MaterialSwitchPreference(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(
            context, attrs, defStyleAttr, defStyleRes)
        {
            WidgetLayoutResource = Resource.Layout.switchPreference;
        }
    }
}