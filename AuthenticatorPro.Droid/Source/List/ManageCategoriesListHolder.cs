using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.List
{
    internal class ManageCategoriesListHolder : RecyclerView.ViewHolder
    {
        public TextView Name { get; }
        public ImageButton MenuButton { get; }


        public ManageCategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);
            MenuButton = itemView.FindViewById<ImageButton>(Resource.Id.buttonMenu);
        }
    }
}