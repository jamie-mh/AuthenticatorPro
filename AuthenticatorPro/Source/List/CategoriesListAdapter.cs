using System;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class CategoriesListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> CategorySelected;

        private int _selectedPosition;

        private readonly Context _context;
        private readonly CategorySource _source;

        public override int ItemCount => _source.View.Count + 1;


        public CategoriesListAdapter(Context context, CategorySource source)
        {
            _context = context;
            _source = source;
            _selectedPosition = -1;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public void SetSelectedPosition(int position)
        {
            _selectedPosition = position + 1;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoriesListHolder) viewHolder;

            holder.Name.Text = position == 0
                ? _context.Resources.GetString(Resource.String.categoryAll)
                : _source.View[position - 1].Name;

            holder.ItemView.Selected = position == _selectedPosition;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemCategory, parent, false);

            var holder = new CategoriesListHolder(itemView);
            holder.Click += (sender, position) =>
            {
                NotifyItemChanged(_selectedPosition);
                _selectedPosition = position;
                NotifyItemChanged(position);

                var categoryId = position == 0
                    ? null
                    : _source.View[position - 1].Id;

                CategorySelected?.Invoke(this, categoryId);
            };

            return holder;
        }
    }
}