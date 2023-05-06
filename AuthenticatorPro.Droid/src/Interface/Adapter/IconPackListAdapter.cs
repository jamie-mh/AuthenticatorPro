// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using AuthenticatorPro.Droid.Persistence.View;
using System;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    internal class IconPackListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<IconPack> DeleteClicked;
        public event EventHandler<IconPack> OpenUrlClicked;
        
        public override int ItemCount => _iconPackView.Count;
        
        private readonly IIconPackView _iconPackView;
        
        public IconPackListAdapter(IIconPackView iconPackView)
        {
            _iconPackView = iconPackView;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var pack = _iconPackView[position];
            
            var holder = (IconPackListHolder) viewHolder;
            holder.Name.Text = pack.Name;
            holder.Description.Text = pack.Description;
            
            holder.Delete.Click += delegate
            {
                DeleteClicked?.Invoke(this, pack);
            };

            holder.OpenUrl.Click += delegate
            {
                OpenUrlClicked?.Invoke(this, pack);
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemIconPack, parent, false);
            return new IconPackListHolder(itemView);
        }
        
        public override long GetItemId(int position)
        {
            return _iconPackView[position].Name.GetHashCode();
        }
    }
}