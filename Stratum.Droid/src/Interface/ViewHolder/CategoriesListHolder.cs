// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.ViewHolder
{
    public class CategoriesListHolder : RecyclerView.ViewHolder
    {
        public CategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);
            itemView.Click += delegate { Clicked?.Invoke(this, BindingAdapterPosition); };
        }

        public MaterialTextView Name { get; }
        public event EventHandler<int> Clicked;
    }
}