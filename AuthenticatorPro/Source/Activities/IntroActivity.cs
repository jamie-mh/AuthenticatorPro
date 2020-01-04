using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Fragment.App;
using AndroidX.Preference;
using AndroidX.ViewPager.Widget;
using AuthenticatorPro.Source.Intro;
using Google.Android.Material.BottomNavigation;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "IntroActivity", Theme = "@style/LightTheme", ScreenOrientation = ScreenOrientation.Portrait)]
    public class IntroActivity : FragmentActivity
    {
        public const int PageCount = 5;

        private ViewPager _pager;
        private PagerAdapter _adapter;
        private BottomNavigationView _nav;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityIntro);

            _pager = FindViewById<ViewPager>(Resource.Id.activityIntro_pager);
            _nav = FindViewById<BottomNavigationView>(Resource.Id.activityIntro_nav);

            _nav.NavigationItemSelected += OnNavigationItemSelected;
            _pager.PageSelected += OnPageSelected;

            _adapter = new IntroPagerAdapter(SupportFragmentManager);
            _pager.Adapter = _adapter;
            OnPageSelected();
        }

        private void OnPageSelected(object sender = null, ViewPager.PageSelectedEventArgs e = null)
        {
            _nav.Menu.FindItem(Resource.Id.intro_prev).SetVisible(_pager.CurrentItem > 0);
            _nav.Menu.FindItem(Resource.Id.intro_next).SetVisible(_pager.CurrentItem < PageCount - 1);
            _nav.Menu.FindItem(Resource.Id.intro_done).SetVisible(_pager.CurrentItem == PageCount - 1);
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
                    var sharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
                    var editor = sharedPrefs.Edit();

                    editor.PutBoolean("firstLaunch", false);
                    editor.Commit();

                    Finish();
                    break;
            }

            OnPageSelected();
        }

        public override void OnBackPressed()
        {
            _pager.CurrentItem--;
        }
    }
}