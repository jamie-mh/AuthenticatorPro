// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.ViewHolder;
using Google.Android.Material.Color;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthenticatorPro.Droid.Interface.Adapter
{
    internal class SheetMenuAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private readonly List<SheetMenuItem> _items;

        public event EventHandler ItemClicked;
        public override int ItemCount => _items.Count;

        public SheetMenuAdapter(Context context, List<SheetMenuItem> items)
        {
            _context = context;
            _items = items;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = _items.ElementAtOrDefault(position);

            if (item == null)
            {
                return;
            }

            var holder = (SheetMenuItemViewHolder) viewHolder;
            holder.ItemView.Click += (sender, args) =>
            {
                item.Handler?.Invoke(sender, args);
                ItemClicked?.Invoke(sender, args);
            };

            holder.Icon.SetImageResource(item.Icon);
            holder.Title.SetText(item.Title);

            if (item.Description != null)
            {
                holder.Description.Visibility = ViewStates.Visible;
                holder.Description.SetText(item.Description.Value);
            }

            if (item.IsSensitive)
            {
                var colourValue = MaterialColors.GetColor(viewHolder.ItemView, Resource.Attribute.colorError);
                var colour = Color.Rgb(Color.GetRedComponent(colourValue), Color.GetBlueComponent(colourValue),
                    Color.GetGreenComponent(colourValue));

                holder.Icon.SetColorFilter(colour);
                holder.Title.SetTextColor(colour);
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemMenu, parent, false);
            return new SheetMenuItemViewHolder(itemView);
        }
    }
}