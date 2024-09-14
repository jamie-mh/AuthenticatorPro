// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.ViewHolder
{
    public class IconListHolder : RecyclerView.ViewHolder
    {
        public IconListHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);

            itemView.Click += delegate { clickListener(BindingAdapterPosition); };
        }

        public ImageView Icon { get; }
        public MaterialTextView Name { get; }
    }
}