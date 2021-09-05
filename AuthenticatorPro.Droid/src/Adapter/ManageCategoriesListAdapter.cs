// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.ViewHolder;
using AuthenticatorPro.Shared.View;
using System;

namespace AuthenticatorPro.Droid.Adapter
{
    internal class ManageCategoriesListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        public event EventHandler<int> MenuClick;
        public event EventHandler MovementFinished;
        public string DefaultId { get; set; }

        private readonly ICategoryView _categoryView;
        public override int ItemCount => _categoryView.Count;

        public ManageCategoriesListAdapter(ICategoryView categoryView)
        {
            _categoryView = categoryView;
        }

        public void MoveItemView(int oldPosition, int newPosition)
        {
            _categoryView.Swap(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public void OnMovementFinished()
        {
            MovementFinished?.Invoke(this, EventArgs.Empty);
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
            holder.MenuButton.Click += delegate { MenuClick(this, holder.BindingAdapterPosition); };

            return holder;
        }
    }
}