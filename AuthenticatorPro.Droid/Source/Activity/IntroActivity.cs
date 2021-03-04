using Android.App;
using Android.OS;
using AndroidX.Fragment.App;
using AndroidX.ViewPager2.Adapter;
using AndroidX.ViewPager2.Widget;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Intro;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.BottomNavigation;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class IntroActivity : FragmentActivity
    {
        private int _pageCount;
        private ViewPager2 _pager;
        private FragmentStateAdapter _adapter;
        private BottomNavigationView _nav;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityIntro);

            _pageCount = Resources.GetStringArray(Resource.Array.introTitle).Length;

            _pager = FindViewById<ViewPager2>(Resource.Id.viewPager);
            _nav = FindViewById<BottomNavigationView>(Resource.Id.navigationView);

            _nav.NavigationItemSelected += OnNavigationItemSelected;

            var callback = new PageChangeCallback();
            callback.PageSelect += delegate { OnPageSelected(); };
            
            _pager.RegisterOnPageChangeCallback(callback);

            _adapter = new IntroPagerAdapter(this, _pageCount);
            _pager.Adapter = _adapter;

            OnPageSelected();
        }

        private void OnPageSelected()
        {
            _nav.Menu.FindItem(Resource.Id.intro_prev).SetVisible(_pager.CurrentItem > 0);
            _nav.Menu.FindItem(Resource.Id.intro_next).SetVisible(_pager.CurrentItem < _pageCount - 1);
            _nav.Menu.FindItem(Resource.Id.intro_done).SetVisible(_pager.CurrentItem == _pageCount - 1);
        }

        private void OnNavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            switch(e.Item.ItemId)
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

        public override void OnBackPressed()
        {
            _pager.CurrentItem--;
        }
    }
}