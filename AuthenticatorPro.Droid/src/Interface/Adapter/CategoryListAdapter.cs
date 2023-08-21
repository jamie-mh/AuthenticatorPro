// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;
using System;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    internal class CategoryListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        public event EventHandler<string> MenuClicked;
        public event EventHandler<bool> MovementFinished;
        public string DefaultId { get; set; }

        private readonly ICategoryView _categoryView;
        public override int ItemCount => _categoryView.Count;

        public CategoryListAdapter(ICategoryView categoryView)
        {
            _categoryView = categoryView;
        }

        public void MoveItemView(int oldPosition, int newPosition)
        {
            _categoryView.Swap(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public void OnMovementFinished(bool orderChanged)
        {
            MovementFinished?.Invoke(this, orderChanged);
        }

        public void OnMovementStarted() { }

        public override long GetItemId(int position)
        {
            return _categoryView[position].Id.GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var category = _categoryView[position];
            var holder = (EditCategoriesListHolder) viewHolder;
            holder.Name.Text = category.Name;
            holder.DefaultImage.Visibility = DefaultId == category.Id
                ? ViewStates.Visible
                : ViewStates.Invisible;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context)
                .Inflate(Resource.Layout.listItemEditCategory, parent, false);

            var holder = new EditCategoriesListHolder(itemView);
            holder.MenuButton.Click += delegate
            {
                var category = _categoryView[holder.BindingAdapterPosition];
                MenuClicked(this, category.Id);
            };

            return holder;
        }
    }
}