using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Utilities.IconList
{
    internal class IconHolder : RecyclerView.ViewHolder
    {
        public IconHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.iconListItem_icon);
            itemView.Click += (sender, e) => clickListener(AdapterPosition);
        }

        public ImageView Icon { get; set; }
    }
}