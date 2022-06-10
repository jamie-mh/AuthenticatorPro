// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.ViewHolder
{
    internal class SheetMenuItemViewHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Title { get; }
        public TextView Description { get; }

        public SheetMenuItemViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            Title = itemView.FindViewById<TextView>(Resource.Id.textTitle);
            Description = itemView.FindViewById<TextView>(Resource.Id.textDescription);
        }
    }
}