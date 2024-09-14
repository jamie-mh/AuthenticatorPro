// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Stratum.Droid.Util;

namespace Stratum.Droid.Interface
{
    public class GridSpacingItemDecoration : RecyclerView.ItemDecoration
    {
        private readonly GridLayoutManager _layoutManager;
        private readonly int _spacing;
        private readonly bool _hasEdgeSpacing;

        public GridSpacingItemDecoration(Context context, GridLayoutManager layoutManager, int spacingDp,
            bool hasEdgeSpacing)
        {
            _layoutManager = layoutManager;
            _spacing = DimenUtil.DpToPx(context, spacingDp);
            _hasEdgeSpacing = hasEdgeSpacing;
        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            var position = parent.GetChildAdapterPosition(view);
            var column = position % _layoutManager.SpanCount;

            if (_hasEdgeSpacing)
            {
                if (column == 0)
                {
                    outRect.Left = _spacing;
                    outRect.Right = _layoutManager.SpanCount > 1 ? _spacing / 2 : _spacing;
                }
                else if (column == _layoutManager.SpanCount - 1)
                {
                    outRect.Left = _layoutManager.SpanCount > 1 ? _spacing / 2 : _spacing;
                    outRect.Right = _spacing;
                }
                else
                {
                    outRect.Left = outRect.Right = _spacing / 2;
                }
            }
            else
            {
                outRect.Left = column * _spacing / _layoutManager.SpanCount;
                outRect.Right = _spacing - (column + 1) * _spacing / _layoutManager.SpanCount;
            }

            outRect.Bottom = _spacing;
        }
    }
}