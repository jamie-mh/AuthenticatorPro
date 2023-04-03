// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Internal;
using System;

namespace AuthenticatorPro.Droid.Interface.LayoutManager
{
    internal class AutoGridLayoutManager : GridLayoutManager
    {
        private readonly float _minColumnWidth;
        private int _lastWidth;
        private int _lastHeight;

        public AutoGridLayoutManager(Context context, int minColumnWidth) : base(context, 1)
        {
            _minColumnWidth = (int) ViewUtils.DpToPx(context, minColumnWidth);
            CalculateSpanCount();
        }

        private void CalculateSpanCount()
        {
            var realWidth = Width + PaddingLeft + PaddingRight;
            var columns = Math.Max(1, (int) Math.Floor(realWidth / _minColumnWidth));
            SpanCount = columns;
        }

        public override void OnLayoutChildren(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            base.OnLayoutChildren(recycler, state);

            if (Width > 0 && Height > 0 && (Width != _lastWidth || Height != _lastHeight))
            {
                _lastWidth = Width;
                _lastHeight = Height;

                CalculateSpanCount();
            }
        }

        public override bool SupportsPredictiveItemAnimations()
        {
            return true;
        }
    }
}