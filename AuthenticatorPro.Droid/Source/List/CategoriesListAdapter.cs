using System;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Data.Source;

namespace AuthenticatorPro.Droid.List
{
    internal sealed class CategoriesListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> CategorySelected;
        public int SelectedPosition { get; set; }

        private readonly Context _context;
        private readonly CategorySource _source;

        public override int ItemCount => _source.GetView().Count + 1;


        public CategoriesListAdapter(Context context, CategorySource source)
        {
            _context = context;
            _source = source;
            SelectedPosition = 0;
        }

        public override long GetItemId(int position)
        {
            return position == 0
                ? -1
                : _source.Get(position - 1).Id.GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoriesListHolder) viewHolder;

            holder.Name.Text = position == 0
                ? _context.Resources.GetString(Resource.String.categoryAll)
                : _source.Get(position - 1).Name;

            holder.ItemView.Selected = position == SelectedPosition;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemCategory, parent, false);

            var holder = new CategoriesListHolder(itemView);
            holder.Click += (_, position) =>
            {
                NotifyItemChanged(SelectedPosition);
                SelectedPosition = position;
                NotifyItemChanged(position);

                var categoryId = position == 0
                    ? null
                    : _source.Get(position - 1).Id;

                CategorySelected?.Invoke(this, categoryId);
            };

            return holder;
        }
    }
}