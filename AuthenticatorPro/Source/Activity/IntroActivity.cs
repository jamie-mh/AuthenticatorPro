using Android.App;
using Android.OS;
using AndroidX.Fragment.App;
using AndroidX.Preference;
using AndroidX.ViewPager.Widget;
using AuthenticatorPro.Intro;
using Google.Android.Material.BottomNavigation;

namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class IntroActivity : FragmentActivity
    {
        private int _pageCount;
        private ViewPager _pager;
        private PagerAdapter _adapter;
        private BottomNavigationView _nav;


        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityIntro);

            _pageCount = Resources.GetStringArray(Resource.Array.introTitle).Length;

            _pager = FindViewById<ViewPager>(Resource.Id.viewPager);
            _nav = FindViewById<BottomNavigationView>(Resource.Id.navigationView);

            _nav.NavigationItemSelected += OnNavigationItemSelected;
            _pager.PageSelected += OnPageSelected;

            _adapter = new IntroPagerAdapter(SupportFragmentManager, _pageCount);
            _pager.Adapter = _adapter;

            OnPageSelected();
        }

        private void OnPageSelected(object sender = null, ViewPager.PageSelectedEventArgs e = null)
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
                    var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    var editor = sharedPrefs.Edit();

                    editor.PutBoolean("firstLaunch", false);
                    editor.Commit();

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