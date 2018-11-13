using System;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities.IconList
{
    internal class IconHolder : Android.Support.V7.Widget.RecyclerView.ViewHolder
    {
        public ImageView Icon { get; set; }

        public IconHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.iconListItem_icon);
            itemView.Click += (sender, e) => clickListener(AdapterPosition);
        }
    }
}