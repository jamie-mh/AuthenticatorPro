// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.TextView;
using System;

namespace AuthenticatorPro.Droid.Interface.ViewHolder
{
    internal class CategoriesListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> Clicked;
        public MaterialTextView Name { get; }

        public CategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<MaterialTextView>(Resource.Id.textName);
            itemView.Click += delegate { Clicked?.Invoke(this, BindingAdapterPosition); };
        }
    }
}