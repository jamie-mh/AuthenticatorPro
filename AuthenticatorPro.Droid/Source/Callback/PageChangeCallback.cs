using System;
using AndroidX.ViewPager2.Widget;

namespace AuthenticatorPro.Droid.Callback
{
    internal class PageChangeCallback : ViewPager2.OnPageChangeCallback
    {
        public event EventHandler<int> PageScrollStateChange;
        public event EventHandler<PageScrollEventArgs> PageScroll;
        public event EventHandler<int> PageSelect;
        
        public override void OnPageScrollStateChanged(int state)
        {
            base.OnPageScrollStateChanged(state);
            PageScrollStateChange?.Invoke(this, state);
        }

        public override void OnPageScrolled(int position, float positionOffset, int positionOffsetPixels)
        {
            base.OnPageScrolled(position, positionOffset, positionOffsetPixels);
            PageScroll?.Invoke(this, new PageScrollEventArgs(position, positionOffset, positionOffsetPixels));
        }

        public override void OnPageSelected(int position)
        {
            base.OnPageSelected(position);
            PageSelect?.Invoke(this, position);
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