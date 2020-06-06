using Android.Content;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class AnimatedGridLayoutManager : GridLayoutManager
    {
        public AnimatedGridLayoutManager(Context context, int spanCount) : base(context, spanCount)
        {

        }

        public override bool SupportsPredictiveItemAnimations()
        {
            return true;
        }
    }
}