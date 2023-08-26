// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    public class CategoryMenuListAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private readonly ICategoryView _categoryView;

        public CategoryMenuListAdapter(Context context, ICategoryView categoryView)
        {
            _context = context;
            _categoryView = categoryView;
            SelectedPosition = 0;
        }

        public int SelectedPosition { get; set; }

        public override int ItemCount => _categoryView.Count + 1;
        public event EventHandler<string> CategorySelected;

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