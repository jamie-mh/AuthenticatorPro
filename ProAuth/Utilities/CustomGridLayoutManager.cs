using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;

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