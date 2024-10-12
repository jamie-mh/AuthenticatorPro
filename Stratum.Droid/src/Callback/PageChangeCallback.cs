// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using AndroidX.ViewPager2.Widget;

namespace Stratum.Droid.Callback
{
    public class PageChangeCallback : ViewPager2.OnPageChangeCallback
    {
        public event EventHandler<int> PageScrollStateChanged;
        public event EventHandler<PageScrollEventArgs> PageScrolled;
        public event EventHandler<int> PageSelected;

        public override void OnPageScrollStateChanged(int state)
        {
            base.OnPageScrollStateChanged(state);
            PageScrollStateChanged?.Invoke(this, state);
        }

        public override void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            base.OnPageScrolled(position, positionOffset, positionOffsetPixels);
            PageScrolled?.Invoke(this, new PageScrollEventArgs(position, positionOffset, positionOffsetPixels));
        }

        public override void OnPageSelected(int position)
        {
            base.OnPageSelected(position);
            PageSelected?.Invoke(this, position);
        }

        public class PageScrollEventArgs : EventArgs
        {
            public readonly int Position;
            public readonly float PositionOffset;
            public readonly int PositionOffsetPixels;

            public PageScrollEventArgs(int position, float positionOffset, int positionOffsetPixels)
            {
                Position = position;
                PositionOffset = positionOffset;
                PositionOffsetPixels = positionOffsetPixels;
            }
        }
    }
}