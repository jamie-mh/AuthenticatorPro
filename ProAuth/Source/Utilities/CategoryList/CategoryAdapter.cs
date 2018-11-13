using System;
using Android.Views;

namespace ProAuth.Utilities.CategoryList
{
    internal sealed class CategoryAdapter : Android.Support.V7.Widget.RecyclerView.Adapter, IAuthAdapterMovement
    {
        private readonly CategorySource _source;

        public event EventHandler<int> RenameClick;
        public event EventHandler<int> DeleteClick;

        public CategoryAdapter(CategorySource source)
        {
            _source = source;
        }

        public override void OnBindViewHolder(Android.Support.V7.Widget.RecyclerView.ViewHolder viewHolder, int position)
        {
            CategoryHolder holder = (CategoryHolder) viewHolder;
            holder.Name.Text = _source.Categories[position].Name;
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.categoryListItem, parent, false);

            CategoryHolder holder = new CategoryHolder(itemView, OnRenameClick, OnDeleteClick);

            return holder;
        }

        public void OnViewMoved(int oldPosition, int newPosition)
        {
            _source.Move(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public override int ItemCount => _source.Count();

        private void OnRenameClick(int position)
        {
            RenameClick?.Invoke(this, position);
        }

        private void OnDeleteClick(int position)
        {
            DeleteClick?.Invoke(this, position);
        }
    }
}