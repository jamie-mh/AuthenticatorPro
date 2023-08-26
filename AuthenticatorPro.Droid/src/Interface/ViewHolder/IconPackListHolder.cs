// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;

namespace AuthenticatorPro.Droid.Interface.ViewHolder
{
    public class IconPackListHolder : RecyclerView.ViewHolder
    {
        public IconPackListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);
            Description = itemView.FindViewById<MaterialTextView>(Resource.Id.textDescription);
            OpenUrl = itemView.FindViewById<ImageButton>(Resource.Id.buttonOpenUrl);
            Delete = itemView.FindViewById<ImageButton>(Resource.Id.buttonDelete);
        }

        public MaterialTextView Name { get; }
        public MaterialTextView Description { get; }
        public ImageButton OpenUrl { get; }
        public ImageButton Delete { get; }
    }
}