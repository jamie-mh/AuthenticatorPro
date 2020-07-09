using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Query;
using AuthenticatorPro.WearOS.Cache;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        private readonly CustomIconCache _customIconCache;
        public List<WearAuthenticatorResponse> Items { get; set; }
        public event EventHandler<int> ItemClick;

        public AuthenticatorListAdapter(CustomIconCache customIconCache)
        {
            _customIconCache = customIconCache;
            Items = new List<WearAuthenticatorResponse>();
        }

        public override long GetItemId(int position)
        {
            return Items[position].GetHashCode();
        }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = Items.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var holder = (AuthenticatorListHolder) viewHolder;
            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;

            if(auth.Icon.StartsWith(CustomIconCache.Prefix))
            {
                var id = auth.Icon.Substring(1);
                var customIcon = await _customIconCache.GetBitmap(id);
                
                if(customIcon != null)
                    holder.Icon.SetImageBitmap(customIcon);
                else
                    holder.Icon.SetImageResource(Icon.GetService(Icon.Default, true));
            }
            else 
                holder.Icon.SetImageResource(Icon.GetService(auth.Icon, true));
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.authListItem, parent, false);
            var holder = new AuthenticatorListHolder(view, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override int ItemCount => Items.Count;
    }
}