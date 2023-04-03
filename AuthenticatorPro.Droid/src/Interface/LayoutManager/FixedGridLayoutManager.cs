// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.Interface.LayoutManager
{
    internal class FixedGridLayoutManager : GridLayoutManager
    {
        public FixedGridLayoutManager(Context context, int spanCount) : base(context, spanCount) { }

        public override bool SupportsPredictiveItemAnimations()
        {
            return false;
        }
    }
}