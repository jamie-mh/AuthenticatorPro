using AndroidX.Fragment.App;
using AuthenticatorPro.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;


namespace AuthenticatorPro.Intro
{
    internal class IntroPagerAdapter : FragmentStatePagerAdapter
    {
        public override int Count { get; }

        public IntroPagerAdapter(FragmentManager manager, int pageCount) : base(manager, BehaviorResumeOnlyCurrentFragment)
        {
            Count = pageCount;
        }

        public override AndroidX.Fragment.App.Fragment GetItem(int position)
        {
            return new IntroPageFragment(position);
        }
    }
}