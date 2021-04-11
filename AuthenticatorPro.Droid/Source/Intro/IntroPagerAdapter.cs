using Android.OS;
using AndroidX.Fragment.App;
using AuthenticatorPro.Droid.Fragment;
using AndroidX.ViewPager2.Adapter;

namespace AuthenticatorPro.Droid.Intro
{
    internal class IntroPagerAdapter : FragmentStateAdapter
    {
        public override int ItemCount { get; }
        
        public IntroPagerAdapter(FragmentActivity activity, int pageCount) : base(activity)
        {
            ItemCount = pageCount;
        }
        
        public override AndroidX.Fragment.App.Fragment CreateFragment(int position)
        {
            var bundle = new Bundle();
            bundle.PutInt("position", position);
            return new IntroPageFragment {Arguments = bundle};
        }
    }
}