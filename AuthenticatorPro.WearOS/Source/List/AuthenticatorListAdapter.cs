using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.WearOS.Cache;
using AuthenticatorPro.WearOS.Data;

namespace AuthenticatorPro.WearOS.List
{
    internal class AuthenticatorListAdapter : RecyclerView.Adapter
    {
        private readonly AuthenticatorSource _authSource;
        private readonly CustomIconCache _customIconCache;
        
        public int? DefaultAuth { get; set; }

        public event EventHandler<int> ItemClick;
        public event EventHandler<int> ItemLongClick;

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

            holder.DefaultImage.Visibility = DefaultAuth != null && auth.Secret.GetHashCode() == DefaultAuth
                ? ViewStates.Visible
                : ViewStates.Gone;

            if(String.IsNullOrEmpty(auth.Username))
                holder.Username.Visibility = ViewStates.Gone;
            else
            {
                holder.Username.Visibility = ViewStates.Visible;
                holder.Username.Text = auth.Username;
            }

            if(!String.IsNullOrEmpty(auth.Icon))
            {
                if(auth.Icon.StartsWith(CustomIconCache.Prefix))
                {
                    var id = auth.Icon[1..];
                    var customIcon = await _customIconCache.GetBitmap(id);
                    
                    if(customIcon != null)
                        holder.Icon.SetImageBitmap(customIcon);
                    else
                        holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
                }
                else 
                    holder.Icon.SetImageResource(IconResolver.GetService(auth.Icon, true));
            }
            else
                holder.Icon.SetImageResource(IconResolver.GetService(IconResolver.Default, true));
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.FromContext(parent.Context).Inflate(Resource.Layout.authListItem, parent, false);
            var holder = new AuthenticatorListHolder(view);
            holder.ItemView.Click += delegate
            {
                ItemClick?.Invoke(this, holder.AdapterPosition);
            };

            holder.ItemView.LongClick += delegate
            {
                ItemLongClick?.Invoke(this, holder.AdapterPosition);
            };

            return holder;
        }

        public override int ItemCount => _authSource.GetView().Count;
    }
}