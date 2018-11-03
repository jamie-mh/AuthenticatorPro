using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace PlusAuth.Utilities
{
    internal class IconHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; set; }

        public IconHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.iconListItem_icon);
            itemView.Click += (sender, e) => clickListener(AdapterPosition);
        }
    }
}