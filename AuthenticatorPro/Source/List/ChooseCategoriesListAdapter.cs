using System;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;

namespace AuthenticatorPro.List
{
    internal sealed class ChooseCategoriesListAdapter : RecyclerView.Adapter
    {
        private readonly CategorySource _categorySource;
        public event EventHandler<int> ItemClick;

        public bool[] CheckedStatus { get; }
        public override int ItemCount => _categorySource.Categories.Count;


        public ChooseCategoriesListAdapter(CategorySource categorySource)
        {
            _categorySource = categorySource;
            CheckedStatus = new bool[_categorySource.Categories.Count];
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var holder = (ChooseCategoriesListHolder) viewHolder;
            var category = _categorySource.Categories[position];

            holder.Name.Text = category.Name;
            holder.CheckBox.Checked = CheckedStatus[position];
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(
                Resource.Layout.chooseCategoriesListItem, parent, false);

            var holder = new ChooseCategoriesListHolder(view);
            holder.ItemClick += (sender, position) =>
            {
                CheckedStatus[position] = !CheckedStatus[position];
                ItemClick?.Invoke(sender, position);
            };

            return holder;
        }
    }
}