using System;
using System.Linq;
using Android.Content;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;

namespace AuthenticatorPro.Utilities.IconList
{
    internal sealed class IconAdapter : RecyclerView.Adapter
    {
        private readonly Context _context;
        private readonly IconSource _iconSource;

        public IconAdapter(Context context, IconSource iconSource)
        {
            _context = context;
            _iconSource = iconSource;
        }

        public override int ItemCount => _iconSource.List.Count;
        public event EventHandler<int> ItemClick;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var icon = _iconSource.List.ElementAt(position);
            var holder = (IconHolder) viewHolder;

            var drawable = ContextCompat.GetDrawable(_context, icon.Value);
            holder.Icon.SetImageDrawable(drawable);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.iconListItem, parent, false);

            var holder = new IconHolder(itemView, OnItemClick);

            return holder;
        }

        private void OnItemClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }

        public override long GetItemId(int position)
        {
            return Icons.Service.ElementAt(position).GetHashCode();
        }
    }
}