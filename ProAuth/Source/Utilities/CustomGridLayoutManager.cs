using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;

namespace ProAuth.Utilities
{
    internal class CustomGridLayoutManager : GridLayoutManager
    {
        public override bool SupportsPredictiveItemAnimations()
        {
            return true;
        }

        protected CustomGridLayoutManager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CustomGridLayoutManager(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public CustomGridLayoutManager(Context context, int spanCount) : base(context, spanCount)
        {
        }

        public CustomGridLayoutManager(Context context, int spanCount, int orientation, bool reverseLayout) : base(context, spanCount, orientation, reverseLayout)
        {
        }
    }
}