using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Data;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        private readonly AuthenticatorSource _authSource;
        private readonly CustomIconCache _customIconCache;
        public event EventHandler<int> ItemClick;

        public AuthenticatorListAdapter(AuthenticatorSource authSource, CustomIconCache customIconCache)
        {
            _authSource = authSource;
            _customIconCache = customIconCache;
        }

        public override long GetItemId(int position)
        {
            return _authSource.Get(position).GetHashCode();
        }

        public override async void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var auth = _authSource.Get(position);

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
            var holder = new AuthenticatorListHolder(view);
            holder.ItemView.Click += delegate
            {
                ItemClick?.Invoke(this, holder.AdapterPosition);
            };

            return holder;
        }

        public override int ItemCount => _authSource.GetView().Count;
    }
}