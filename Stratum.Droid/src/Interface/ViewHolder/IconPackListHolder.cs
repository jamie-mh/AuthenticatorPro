// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.ViewHolder
{
    public class IconPackListHolder : RecyclerView.ViewHolder
    {
        public IconPackListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);
            Description = itemView.FindViewById<MaterialTextView>(Resource.Id.textDescription);
            ViewSource = itemView.FindViewById<MaterialButton>(Resource.Id.buttonViewSource);
            Delete = itemView.FindViewById<MaterialButton>(Resource.Id.buttonDelete);
        }

        public MaterialTextView Name { get; }
        public MaterialTextView Description { get; }
        public MaterialButton ViewSource { get; }
        public MaterialButton Delete { get; }
    }
}