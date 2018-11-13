using System;
using Android.Views;

namespace ProAuth.Utilities.CategoryList
{
    internal sealed class ChooseCategoriesAdapter : Android.Support.V7.Widget.RecyclerView.Adapter
    {
        public Action<bool, int> ItemClick;
        public bool[] CheckedStatus { get; }

        private readonly CategorySource _categorySource;

        public ChooseCategoriesAdapter(CategorySource categorySource)
        {
            _categorySource = categorySource;
            CheckedStatus = new bool[_categorySource.Count()];
        }

        public override void OnBindViewHolder(Android.Support.V7.Widget.RecyclerView.ViewHolder viewHolder, int position)
        {
            ChooseCategoriesHolder holder = (ChooseCategoriesHolder) viewHolder;
            Data.Category category = _categorySource.Categories[position];

            holder.Name.Text = category.Name;
            holder.CheckBox.Checked = CheckedStatus[position];
        }

        public override Android.Support.V7.Widget.RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.chooseCategoriesListItem, parent, false);

            ChooseCategoriesHolder holder = new ChooseCategoriesHolder(itemView, ItemClick);

            return holder;
        }

        public override int ItemCount => _categorySource.Count();
    }
}