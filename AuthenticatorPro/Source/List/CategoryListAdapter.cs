using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class CategoryListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        public event EventHandler<int> MenuClick;
        private readonly CategorySource _source;

        public override int ItemCount => _source.Categories.Count;


        public CategoryListAdapter(CategorySource source)
        {
            _source = source;
        }

        public void MoveItem(int oldPosition, int newPosition)
        {
            _source.Move(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public void NotifyMovementFinished()
        {

        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (CategoryListHolder) viewHolder;
            holder.Name.Text = _source.Categories[position].Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemCategory, parent, false);

            var holder = new CategoryListHolder(itemView);
            holder.MenuClick += MenuClick;

            return holder;
        }
    }
}