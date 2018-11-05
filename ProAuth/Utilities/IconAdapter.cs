using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Views;

namespace ProAuth.Utilities
{
    internal sealed class IconAdapter : RecyclerView.Adapter
    {
        public event EventHandler<int> ItemClick;
        private readonly Context _context;
        private readonly IconSource _iconSource;

        public IconAdapter(Context context, IconSource iconSource)
        {
            _context = context;
            _iconSource = iconSource;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            KeyValuePair<string, int> icon = _iconSource.List.ElementAt(position);
            IconHolder holder = (IconHolder) viewHolder;

            Drawable drawable = ContextCompat.GetDrawable(_context, icon.Value);
            holder.Icon.SetImageDrawable(drawable);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.iconListItem, parent, false);

            IconHolder holder = new IconHolder(itemView, OnItemClick);

            return holder;
        }

        public override int ItemCount => _iconSource.List.Count;

        private void OnItemClick(int position)
        {
            ItemClick?.Invoke(this, position);
        }
    }
}