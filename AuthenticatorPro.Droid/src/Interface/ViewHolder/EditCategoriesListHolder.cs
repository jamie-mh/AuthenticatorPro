// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.Interface.ViewHolder
{
    internal class EditCategoriesListHolder : RecyclerView.ViewHolder
    {
        public TextView Name { get; }
        public ImageView DefaultImage { get; }
        public ImageButton MenuButton { get; }

        public EditCategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);
            DefaultImage = itemView.FindViewById<ImageView>(Resource.Id.imageDefault);
            MenuButton = itemView.FindViewById<ImageButton>(Resource.Id.buttonMenu);
        }
    }
}