// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.ViewHolder;
using AuthenticatorPro.Shared.View;
using System;

namespace AuthenticatorPro.Droid.Adapter
{
    internal class CategoriesListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> CategorySelected;
        public int SelectedPosition { get; set; }

        private readonly Context _context;
        private readonly ICategoryView _categoryView;

        public override int ItemCount => _categoryView.Count + 1;

        public CategoriesListAdapter(Context context, ICategoryView categoryView)
        {
            _context = context;
            _categoryView = categoryView;
            SelectedPosition = 0;
        }

        public override long GetItemId(int position)
        {
            return position == 0
                ? -1
                : -_categoryView[position - 1].GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoriesListHolder) viewHolder;

            holder.Name.Text = position == 0
                ? _context.Resources.GetString(Resource.String.categoryAll)
                : _categoryView[position - 1].Name;

            holder.ItemView.Selected = position == SelectedPosition;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemCategory, parent, false);

            var holder = new CategoriesListHolder(itemView);
            holder.Clicked += (_, position) =>
            {
                NotifyItemChanged(SelectedPosition);
                SelectedPosition = position;
                NotifyItemChanged(position);

                var categoryId = position == 0 ? null : _categoryView[position - 1].Id;
                CategorySelected?.Invoke(this, categoryId);
            };

            return holder;
        }
    }
}