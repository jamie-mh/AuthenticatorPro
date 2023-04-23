// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;
using System;

namespace AuthenticatorPro.Droid.Interface.ViewHolder
{
    internal class IconListHolder : RecyclerView.ViewHolder
    {
        public ImageView Icon { get; }
        public MaterialTextView Name { get; }

        public IconListHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);

            itemView.Click += delegate { clickListener(BindingAdapterPosition); };
        }
    }
}