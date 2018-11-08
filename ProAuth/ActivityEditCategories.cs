using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android;
using Android.Content;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Permission = Android.Content.PM.Permission;
using Android.Runtime;
using Android.Support.V7.Widget;
using ProAuth.Utilities;

namespace ProAuth
{
    [Activity(Label = "EditCategoriesActivity")]
    public class ActivityEditCategories: AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityEditCategories);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityEditCategories_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.editCategories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            RecyclerView list = FindViewById<RecyclerView>(Resource.Id.activityEditCategories_list);
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}