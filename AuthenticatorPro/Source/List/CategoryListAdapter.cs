using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class CategoryListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        private readonly CategorySource _source;

        public CategoryListAdapter(CategorySource source)
        {
            _source = source;
        }

        public override int ItemCount => _source.Count();

        public void MoveItem(int oldPosition, int newPosition)
        {
            _source.Move(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public void NotifyMovementFinished()
        {

        }

        public event EventHandler<int> RenameClick;
        public event EventHandler<int> DeleteClick;

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoryListHolder) viewHolder;
            holder.Name.Text = _source.Categories[position].Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.categoryListItem, parent, false);

            var holder = new CategoryListHolder(itemView, OnRenameClick, OnDeleteClick);

            return holder;
        }

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