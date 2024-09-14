// Copyright (C) 2024 jmh
// SPDX-License-Identifier:GPL-3.0-only

using Android.Content;
using Android.Util;

namespace Stratum.Droid.Util
{
    public static class DimenUtil
    {
        public static int DpToPx(Context context, int dp)
        {
            return (int) TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, context.Resources.DisplayMetrics);
        }
    }
}