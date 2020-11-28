using Android.Content;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class FixedGridLayoutManager : GridLayoutManager
    {
        public FixedGridLayoutManager(Context context, int spanCount) : base(context, spanCount)
        {

        }

        public override bool SupportsPredictiveItemAnimations()
        {
            return false;
        }
    }
}