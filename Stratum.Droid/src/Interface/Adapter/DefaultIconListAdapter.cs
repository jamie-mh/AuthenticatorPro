// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using Stratum.Droid.Interface.ViewHolder;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid.Interface.Adapter
{
    public class DefaultIconListAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private readonly IDefaultIconView _defaultIconView;

        public DefaultIconListAdapter(Context context, IDefaultIconView defaultIconView)
        {
            _context = context;
            _defaultIconView = defaultIconView;
        }

        public override int ItemCount => _defaultIconView.Count;
        public event EventHandler<int> ItemClicked;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var (key, value) = _defaultIconView[position];
            var holder = (IconListHolder) viewHolder;

            var drawable = ContextCompat.GetDrawable(_context, value);
            holder.Icon.SetImageDrawable(drawable);
            TooltipCompat.SetTooltipText(holder.ItemView, key);
            holder.Name.Text = key;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemIcon, parent, false);
            var holder = new IconListHolder(itemView, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            ItemClicked?.Invoke(this, position);
        }

        public override long GetItemId(int position)
        {
            return _defaultIconView[position].Key.GetHashCode();
        }
    }
}