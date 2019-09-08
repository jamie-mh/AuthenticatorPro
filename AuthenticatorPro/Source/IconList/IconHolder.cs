using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.IconList
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