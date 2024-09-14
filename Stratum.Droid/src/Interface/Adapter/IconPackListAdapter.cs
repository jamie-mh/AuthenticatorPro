// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Stratum.Core.Entity;
using Stratum.Droid.Interface.ViewHolder;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid.Interface.Adapter
{
    public class IconPackListAdapter : RecyclerView.Adapter
    {
        private readonly IIconPackView _iconPackView;

        public IconPackListAdapter(IIconPackView iconPackView)
        {
            _iconPackView = iconPackView;
        }

        public override int ItemCount => _iconPackView.Count;
        public event EventHandler<IconPack> DeleteClicked;
        public event EventHandler<IconPack> ViewSourceClicked;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var pack = _iconPackView[position];

            var holder = (IconPackListHolder) viewHolder;
            holder.Name.Text = pack.Name;
            holder.Description.Text = pack.Description;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemIconPack, parent, false);
            var holder = new IconPackListHolder(itemView);

            holder.Delete.Click += delegate
            {
                var pack = _iconPackView[holder.BindingAdapterPosition];
                DeleteClicked?.Invoke(this, pack);
            };

            holder.ViewSource.Click += delegate
            {
                var pack = _iconPackView[holder.BindingAdapterPosition];
                ViewSourceClicked?.Invoke(this, pack);
            };

            return holder;
        }

        public override long GetItemId(int position)
        {
            return _iconPackView[position].Name.GetHashCode();
        }
    }
}