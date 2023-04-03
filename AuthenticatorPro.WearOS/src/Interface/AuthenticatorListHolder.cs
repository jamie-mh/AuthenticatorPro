// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.WearOS.Interface
{
    internal class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public TextView Username { get; }
        public TextView Issuer { get; }
        public ImageView Icon { get; }
        public ImageView DefaultImage { get; }

        public AuthenticatorListHolder(View itemView) : base(itemView)
        {
            Issuer = itemView.FindViewById<TextView>(Resource.Id.textIssuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.textUsername);
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            DefaultImage = itemView.FindViewById<ImageView>(Resource.Id.imageDefault);
        }
    }
}