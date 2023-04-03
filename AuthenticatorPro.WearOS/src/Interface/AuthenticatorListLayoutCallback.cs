// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;
using System;

namespace AuthenticatorPro.WearOS.Interface
{
    internal class AuthenticatorListLayoutCallback : CurvingLayoutCallback
    {
        private const float MaxIconProgress = .65f;

        public AuthenticatorListLayoutCallback(Context context) : base(context) { }

        public override void OnLayoutFinished(View child, RecyclerView parent)
        {
            base.OnLayoutFinished(child, parent);

            var centerOffset = child.Height / 2f / parent.Height;
            var yRelativeToCenterOffset = (child.GetY() / parent.Height) + centerOffset;

            var progressToCenter = Math.Min(Math.Abs(.5f - yRelativeToCenterOffset), MaxIconProgress);
            var icon = child.FindViewById<ImageView>(Resource.Id.imageIcon);

            icon.ScaleX = 1 - progressToCenter;
            icon.ScaleY = 1 - progressToCenter;
        }
    }
}