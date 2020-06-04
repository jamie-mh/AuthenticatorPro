using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.WearOS.AuthenticatorList
{
    class AuthHolder : RecyclerView.ViewHolder
    {
        public TextView Username { get; }
        public TextView Issuer { get; }
        public ImageView Icon { get; }

        public AuthHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            itemView.Click += (sender, e) => clickListener(AdapterPosition);

            Issuer = itemView.FindViewById<TextView>(Resource.Id.authListItem_issuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.authListItem_username);
            Icon = itemView.FindViewById<ImageView>(Resource.Id.authListItem_icon);
        }
    }
}