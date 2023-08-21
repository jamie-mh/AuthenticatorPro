// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Graphics;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;
using System;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    internal class IconPackEntryListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<Bitmap> ItemClicked;
        public override int ItemCount => _iconPackEntryView.Count;

        private readonly IIconPackEntryView _iconPackEntryView;
        
        public IconPackEntryListAdapter(IIconPackEntryView iconPackEntryView)
        {
            _iconPackEntryView = iconPackEntryView;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var (id, bitmap) = _iconPackEntryView[position];
            var holder = (IconListHolder) viewHolder;

            TooltipCompat.SetTooltipText(holder.ItemView, id);
            holder.Name.Text = id;
            holder.Icon.SetImageBitmap(bitmap);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemIcon, parent, false);
            return new IconListHolder(itemView, OnItemClick);
        }

        private void OnItemClick(int position)
        {
            var (_, bitmap) = _iconPackEntryView[position];
            ItemClicked?.Invoke(this, bitmap);
        }

        public override long GetItemId(int position)
        {
            return _iconPackEntryView[position].Key.GetHashCode();
        }
    }
}