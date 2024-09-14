// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.OS;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;
using Google.Android.Material.BottomNavigation;
using Google.Android.Material.Navigation;
using Stratum.Droid.Callback;
using Stratum.Droid.Interface.Adapter;

namespace Stratum.Droid.Activity
{
    [Activity]
    public class IntroActivity : BaseActivity
    {
        private int _pageCount;
        private ViewPager2 _pager;
        private FragmentStateAdapter _adapter;
        private BottomNavigationView _nav;

        public IntroActivity() : base(Resource.Layout.activityIntro)
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _pageCount = Resources.GetStringArray(Resource.Array.introTitle).Length;

            _pager = FindViewById<ViewPager2>(Resource.Id.viewPager);
            _nav = FindViewById<BottomNavigationView>(Resource.Id.navigationView);

            _nav.ItemSelected += OnItemSelected;

            var pageChangeCallback = new PageChangeCallback();
            pageChangeCallback.PageSelected += delegate { OnPageSelected(); };

            _pager.RegisterOnPageChangeCallback(pageChangeCallback);

            _adapter = new IntroPagerAdapter(this, _pageCount);
            _pager.Adapter = _adapter;

            var backPressCallback = new BackPressCallback(true);
            backPressCallback.BackPressed += delegate { _pager.CurrentItem--; };

            OnBackPressedDispatcher.AddCallback(backPressCallback);

            OnPageSelected();
        }

        private void OnPageSelected()
        {
            _nav.Menu.FindItem(Resource.Id.intro_prev).SetVisible(_pager.CurrentItem > 0);
            _nav.Menu.FindItem(Resource.Id.intro_next).SetVisible(_pager.CurrentItem < _pageCount - 1);
            _nav.Menu.FindItem(Resource.Id.intro_done).SetVisible(_pager.CurrentItem == _pageCount - 1);
        }

        private void OnItemSelected(object sender, NavigationBarView.ItemSelectedEventArgs e)
        {
            switch (e.P0.ItemId)
            {
                case Resource.Id.intro_prev:
                    _pager.CurrentItem--;
                    break;

                case Resource.Id.intro_next:
                    _pager.CurrentItem++;
                    break;

                case Resource.Id.intro_done:
                {
                    _ = new PreferenceWrapper(this) { FirstLaunch = false };
                    Finish();
                    break;
                }
            }

            OnPageSelected();
        }
    }
}