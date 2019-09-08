using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AuthenticatorPro.Activities;
using AuthenticatorPro.Source.Fragments;
using Fragment = AndroidX.Fragment.App.Fragment;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace AuthenticatorPro.Source.Intro
{
    internal class IntroPagerAdapter : FragmentStatePagerAdapter
    {
        public IntroPagerAdapter(FragmentManager manager) : base(manager) { }

        public override int Count => IntroActivity.PageCount;

        public override Fragment GetItem(int position)
        {
            return new IntroPageFragment(position);
        }
    }
}