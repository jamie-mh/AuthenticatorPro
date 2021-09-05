// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using AndroidX.Fragment.App;
using AndroidX.ViewPager2.Adapter;
using AuthenticatorPro.Droid.Fragment;

namespace AuthenticatorPro.Droid.Adapter
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
            return new IntroPageFragment { Arguments = bundle };
        }
    }
}