using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class ManageCategoriesListAdapter : RecyclerView.Adapter, IReorderableListAdapter
    {
        public event EventHandler<int> MenuClick;
        private readonly CategorySource _source;

        public override int ItemCount => _source.View.Count;


        public ManageCategoriesListAdapter(CategorySource source)
        {
            _source = source;
        }

        public void MoveItem(int oldPosition, int newPosition)
        {
            _source.Move(oldPosition, newPosition);
            NotifyItemMoved(oldPosition, newPosition);
        }

        public void OnMovementFinished()
        {

        }

        public void OnMovementStarted()
        {

        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (ManageCategoriesListHolder) viewHolder;
            holder.Name.Text = _source.View[position].Name;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.listItemManageCategory, parent, false);

            var holder = new ManageCategoriesListHolder(itemView);
            holder.MenuClick += MenuClick;

            return holder;
        }
    }
}