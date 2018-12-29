using System;
using Android.Content;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;

namespace AuthenticatorPro.Utilities
{
    internal class CustomGridLayoutManager : GridLayoutManager
    {
        protected CustomGridLayoutManager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference,
            transfer)
        {
        }

        public CustomGridLayoutManager(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(
            context, attrs, defStyleAttr, defStyleRes)
        {
        }

        public CustomGridLayoutManager(Context context, int spanCount) : base(context, spanCount)
        {
        }

        public CustomGridLayoutManager(Context context, int spanCount, int orientation, bool reverseLayout) : base(
            context, spanCount, orientation, reverseLayout)
        {
        }

        public override bool SupportsPredictiveItemAnimations()
        {
            return true;
        }
    }
}