using System;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using OtpSharp;
using ProAuth.Data;

namespace ProAuth.Utilities
{
    internal sealed class CategoryAdapter : RecyclerView.Adapter, IAuthAdapterMovement
    {
        private readonly CategorySource _source;

        public event EventHandler<int> RenameClick;
        public event EventHandler<int> DeleteClick;

        public CategoryAdapter(CategorySource source)
        {
            _source = source;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            CategoryHolder holder = (CategoryHolder) viewHolder;
            holder.Name.Text = _source.Categories[position].Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
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