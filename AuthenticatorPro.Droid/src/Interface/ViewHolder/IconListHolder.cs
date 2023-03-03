// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;

namespace AuthenticatorPro.Droid.Interface.ViewHolder
{
    internal class IconListHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public TextView Name { get; }

        public IconListHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);

            itemView.Click += delegate { clickListener(BindingAdapterPosition); };
        }
    }
}