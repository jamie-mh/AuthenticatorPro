using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class ChooseCategoriesListAdapter : RecyclerView.Adapter
    {
        private readonly CategorySource _categorySource;
        public Action<bool, int> ItemClick;

        public ChooseCategoriesListAdapter(CategorySource categorySource)
        {
            _categorySource = categorySource;
            CheckedStatus = new bool[_categorySource.Count()];
        }

        public bool[] CheckedStatus { get; }

        public override int ItemCount => _categorySource.Count();

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (ChooseCategoriesListHolder) viewHolder;
            var category = _categorySource.Categories[position];

            holder.Name.Text = category.Name;
            holder.CheckBox.Checked = CheckedStatus[position];
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.chooseCategoriesListItem, parent, false);

            var holder = new ChooseCategoriesListHolder(itemView, ItemClick);

            return holder;
        }
    }
}