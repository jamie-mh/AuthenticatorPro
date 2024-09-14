// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using AndroidX.Fragment.App;
using AndroidX.ViewPager2.Adapter;
using Stratum.Droid.Interface.Fragment;

namespace Stratum.Droid.Interface.Adapter
{
    public class IntroPagerAdapter : FragmentStateAdapter
    {
        public IntroPagerAdapter(FragmentActivity activity, int pageCount) : base(activity)
        {
            ItemCount = pageCount;
        }

        public override int ItemCount { get; }

        public override AndroidX.Fragment.App.Fragment CreateFragment(int position)
        {
            var bundle = new Bundle();
            bundle.PutInt("position", position);
            return new IntroPageFragment { Arguments = bundle };
        }
    }
}