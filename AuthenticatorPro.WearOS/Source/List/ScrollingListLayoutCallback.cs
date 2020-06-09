using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AndroidX.Wear.Widget;

namespace AuthenticatorPro.WearOS.List
{
    // https://developer.android.com/training/wearables/ui/lists#creating
    internal class ScrollingListLayoutCallback : WearableLinearLayoutManager.LayoutCallback
    {
        private const float MaxIconProgress = .65f;
        private const int XOffset = 60;

        private readonly bool _isRound;


        public ScrollingListLayoutCallback(bool isRound)
        {
            _isRound = isRound;
        }

        public override void OnLayoutFinished(View child, RecyclerView parent)
        {
            var centerOffset = child.Height / 2f / parent.Height;
            var yRelativeToCenterOffset = child.GetY() / parent.Height + centerOffset;

            var progressToCenterX = (float) Math.Sin(yRelativeToCenterOffset * Math.PI);
            var progressToCenterY = Math.Min(Math.Abs(.5f - yRelativeToCenterOffset), MaxIconProgress);

            child.ScaleX = 1 - progressToCenterY;
            child.ScaleY = 1 - progressToCenterY;

            if(_isRound)
                child.SetX(Math.Abs(1 - progressToCenterX) * XOffset);
        }
    }
}