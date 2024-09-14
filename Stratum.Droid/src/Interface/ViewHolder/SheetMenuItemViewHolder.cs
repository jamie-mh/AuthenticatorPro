// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.ViewHolder
{
    public class SheetMenuItemViewHolder : RecyclerView.ViewHolder
    {
        public SheetMenuItemViewHolder(View itemView) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.imageIcon);
            Title = itemView.FindViewById<MaterialTextView>(Resource.Id.textTitle);
            Description = itemView.FindViewById<MaterialTextView>(Resource.Id.textDescription);
        }

        public ImageView Icon { get; }
        public MaterialTextView Title { get; }
        public MaterialTextView Description { get; }
    }
}