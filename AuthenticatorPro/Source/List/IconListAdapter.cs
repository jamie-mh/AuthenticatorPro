using System;
using Android.Content;
using Android.Views;
using AndroidX.AppCompat.Widget;
using AndroidX.Core.Content;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class IconListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        public override int ItemCount => _iconSource.GetView().Count;

        private readonly Context _context;
        private readonly IconSource _iconSource;

        public IconListAdapter(Context context, IconSource iconSource)
        {
            _context = context;
            _iconSource = iconSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var (key, value) = _iconSource.Get(position);
            var holder = (IconListHolder) viewHolder;

            var drawable = ContextCompat.GetDrawable(_context, value);
            holder.Icon.SetImageDrawable(drawable);
            TooltipCompat.SetTooltipText(holder.ItemView, key);
            holder.Name.Text = key;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemIcon, parent, false);
            var holder = new IconListHolder(itemView, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override long GetItemId(int position)
        {
            return _iconSource.Get(position).GetHashCode();
        }
    }
}