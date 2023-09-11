// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.OS;
using Android.Views;
using AndroidX.Core.Graphics;
using AndroidX.Core.Widget;
using Google.Android.Material.AppBar;

#if FDROID
using Google.Android.Material.Card;
#endif

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    public class GuideActivity : BaseActivity
    {
        public GuideActivity() : base(Resource.Layout.activityGuide)
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var toolbar = FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.gettingStartedGuide);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

#if FDROID
            var wearOsCard = FindViewById<MaterialCardView>(Resource.Id.cardWearOS);
            wearOsCard.Visibility = ViewStates.Gone;
#endif
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            base.OnApplySystemBarInsets(insets);
            var scrollView = FindViewById<NestedScrollView>(Resource.Id.nestedScrollView);
            scrollView.SetPadding(0, 0, 0, insets.Bottom);
        }
    }
}