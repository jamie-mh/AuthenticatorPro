// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Color;
using Stratum.Droid.Interface.ViewHolder;

namespace Stratum.Droid.Interface.Adapter
{
    public class SheetMenuAdapter : RecyclerView.Adapter
    {
        private readonly List<SheetMenuItem> _items;

        public SheetMenuAdapter(List<SheetMenuItem> items)
        {
            _items = items;
        }

        public override int ItemCount => _items.Count;

        public event EventHandler ItemClicked;

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