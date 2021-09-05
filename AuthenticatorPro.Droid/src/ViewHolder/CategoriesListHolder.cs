// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using System;

namespace AuthenticatorPro.Droid.ViewHolder
{
    internal class CategoriesListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> Click;
        public TextView Name { get; }

        public CategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);
            itemView.Click += delegate { Click?.Invoke(this, BindingAdapterPosition); };
        }
    }
}