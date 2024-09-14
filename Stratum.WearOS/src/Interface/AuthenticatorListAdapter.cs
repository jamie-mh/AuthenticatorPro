// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Stratum.Core.Util;
using Stratum.Droid.Shared;
using Stratum.WearOS.Cache;
using Stratum.WearOS.Cache.View;

namespace Stratum.WearOS.Interface
{
    public class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        private readonly AuthenticatorView _authView;
        private readonly CustomIconCache _customIconCache;
        private readonly bool _showUsernames;

        public AuthenticatorListAdapter(AuthenticatorView authView, CustomIconCache customIconCache, bool showUsernames)
        {
            _authView = authView;
            _customIconCache = customIconCache;
            _showUsernames = showUsernames;
        }

        public string DefaultAuth { get; set; }

        public override int ItemCount => _authView.Count;

        public event EventHandler<int> ItemClicked;
        public event EventHandler<int> ItemLongClicked;

        public override long GetItemId(int position)
        {
            return _authView[position].GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = _authView[position];

            if (auth == null)
            {
                return;
            }

            var holder = (AuthenticatorListHolder) viewHolder;
            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            holder.DefaultImage.Visibility = DefaultAuth != null && HashUtil.Sha1(auth.Secret) == DefaultAuth
                ? ViewStates.Visible
                : ViewStates.Gone;

            if (!_showUsernames)
            {
                var uniqueIssuer = _authView.Count(a => a.Issuer == auth.Issuer) == 1;
                holder.Username.Visibility = uniqueIssuer
                    ? ViewStates.Gone
                    : ViewStates.Visible;
            }
            else
            {
                holder.Username.Visibility = string.IsNullOrEmpty(auth.Username)
                    ? ViewStates.Gone
                    : ViewStates.Visible;
            }

            if (!string.IsNullOrEmpty(auth.Icon))
            {
                if (auth.Icon.StartsWith(CustomIconCache.Prefix))
                {
                    var id = auth.Icon[1..];
                    var customIcon = _customIconCache.GetCachedBitmap(id);

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
            holder.ItemView.Click += delegate { ItemClicked?.Invoke(this, holder.BindingAdapterPosition); };

            holder.ItemView.LongClick += delegate { ItemLongClicked?.Invoke(this, holder.BindingAdapterPosition); };

            return holder;
        }
    }
}