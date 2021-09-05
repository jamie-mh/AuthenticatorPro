// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Data;
using System;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        private readonly AuthenticatorView _authView;
        private readonly CustomIconCache _customIconCache;

        public int? DefaultAuth { get; set; }

        public event EventHandler<int> ItemClicked;
        public event EventHandler<int> ItemLongClicked;

        public AuthenticatorListAdapter(AuthenticatorView authView, CustomIconCache customIconCache)
        {
            _authView = authView;
            _customIconCache = customIconCache;
        }

        public override long GetItemId(int position)
        {
            return _authView[position].GetHashCode();
        }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = _authView[position];

            if (auth == null)
            {
                return;
            }

            var holder = (AuthenticatorListHolder) viewHolder;
            holder.Issuer.Text = auth.Issuer;

            holder.DefaultImage.Visibility = DefaultAuth != null && auth.Secret.GetHashCode() == DefaultAuth
                ? ViewStates.Visible
                : ViewStates.Gone;

            if (String.IsNullOrEmpty(auth.Username))
            {
                holder.Username.Visibility = ViewStates.Gone;
            }
            else
            {
                holder.Username.Visibility = ViewStates.Visible;
                holder.Username.Text = auth.Username;
            }

            if (!String.IsNullOrEmpty(auth.Icon))
            {
                if (auth.Icon.StartsWith(CustomIconCache.Prefix))
                {
                    var id = auth.Icon[1..];
                    var customIcon = await _customIconCache.GetBitmap(id);

                    if (customIcon != null)
                    {
                        holder.Icon.SetImageBitmap(customIcon);
                    }
                    else
                    {
                        holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
                    }
                }
                else
                {
                    holder.Icon.SetImageResource(IconResolver.GetService(auth.Icon, true));
                }
            }
            else
            {
                holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.authListItem, parent, false);
            var holder = new AuthenticatorListHolder(view);
            holder.ItemView.Click += delegate
            {
                ItemClicked?.Invoke(this, holder.BindingAdapterPosition);
            };

            holder.ItemView.LongClick += delegate
            {
                ItemLongClicked?.Invoke(this, holder.BindingAdapterPosition);
            };

            return holder;
        }

        public override int ItemCount => _authView.Count;
    }
}