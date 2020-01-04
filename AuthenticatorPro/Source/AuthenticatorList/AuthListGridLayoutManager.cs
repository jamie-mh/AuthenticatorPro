using Android.Content;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.AuthenticatorList
{
    internal class AuthListGridLayoutManager : GridLayoutManager
    {
        public AuthListGridLayoutManager(Context context, int spanCount) : base(context, spanCount)
        {

        }

        public override bool SupportsPredictiveItemAnimations()
        {
            return true;
        }
    }
}