using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public TextView Username { get; }
        public TextView Issuer { get; }
        public ImageView Icon { get; }

        public AuthenticatorListHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            itemView.Click += (sender, e) => clickListener(AdapterPosition);

            Issuer = itemView.FindViewById<TextView>(Resource.Id.textIssuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.textUsername);
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
        }
    }
}