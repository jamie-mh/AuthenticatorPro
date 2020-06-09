using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Query;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        public List<WearAuthenticatorResponse> Items { get; set; }
        public event EventHandler<int> ItemClick;

        public AuthenticatorListAdapter()
        {
            Items = new List<WearAuthenticatorResponse>();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = Items.ElementAtOrDefault(position);

            if(auth == null)
                return;

            var holder = (AuthenticatorListHolder) viewHolder;
            holder.Issuer.Text = auth.Issuer;
            holder.Username.Text = auth.Username;
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