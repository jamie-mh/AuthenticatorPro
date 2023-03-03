// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;
using System;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    internal class IconListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClicked;
        public override int ItemCount => _iconView.Count;

        private readonly Context _context;
        private readonly IIconView _iconView;

        public IconListAdapter(Context context, IIconView iconView)
        {
            _context = context;
            _iconView = iconView;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var (key, value) = _iconView[position];
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
            return _iconView[position].Key.GetHashCode();
        }
    }
}