using System;
using Android.Support.V7.Widget;
using Android.Support.Wear.Widget;
using Android.Views;

namespace AuthenticatorPro.WearOS.AuthenticatorList
{
    // https://developer.android.com/training/wearables/ui/lists#creating
    class AuthScrollingLayoutCallback : WearableLinearLayoutManager.LayoutCallback
    {
        private const float MaxIconProgress = .65f;
        private const int XOffset = 60;

        private readonly bool _isRound;


        public AuthScrollingLayoutCallback(bool isRound)
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