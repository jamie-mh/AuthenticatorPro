using System;
using Android.Content;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.List
{
    internal sealed class CategoriesListAdapter : RecyclerView.Adapter
    {
        public event EventHandler<string> CategorySelected;
        public int SelectedPosition { get; set; }

        private readonly Context _context;
        private readonly string[] _categoryIds;
        private readonly string[] _categoryNames;

        public override int ItemCount => _categoryIds.Length + 1;

        public CategoriesListAdapter(Context context, string[] categoryIds, string[] categoryNames)
        {
            _context = context;
            _categoryIds = categoryIds;
            _categoryNames = categoryNames;
            
            SelectedPosition = 0;
        }

        public override long GetItemId(int position)
        {
            return position == 0
                ? -1
                : _categoryIds[position - 1].GetHashCode();
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoriesListHolder) viewHolder;

            holder.Name.Text = position == 0
                ? _context.Resources.GetString(Resource.String.categoryAll)
                : _categoryNames[position - 1];

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

                var categoryId = position == 0 ? null : _categoryIds[position - 1];
                CategorySelected?.Invoke(this, categoryId);
            };

            return holder;
        }
    }
}