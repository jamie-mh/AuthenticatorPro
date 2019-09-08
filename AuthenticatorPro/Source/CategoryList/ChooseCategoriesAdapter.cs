using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.CategoryList
{
    internal sealed class ChooseCategoriesAdapter : RecyclerView.Adapter
    {
        private readonly CategorySource _categorySource;
        public Action<bool, int> ItemClick;

        public ChooseCategoriesAdapter(CategorySource categorySource)
        {
            _categorySource = categorySource;
            CheckedStatus = new bool[_categorySource.Count()];
        }

        public bool[] CheckedStatus { get; }

        public override int ItemCount => _categorySource.Count();

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (ChooseCategoriesHolder) viewHolder;
            var category = _categorySource.Categories[position];

            holder.Name.Text = category.Name;
            holder.CheckBox.Checked = CheckedStatus[position];
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var itemView = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.chooseCategoriesListItem, parent, false);

            var holder = new ChooseCategoriesHolder(itemView, ItemClick);

            return holder;
        }
    }
}