using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities
{
    internal class ChooseCategoriesHolder : RecyclerView.ViewHolder
    {
        public TextView Name { get; }
        public CheckBox CheckBox { get; }

        public ChooseCategoriesHolder(View itemView, Action<bool, int> onClick) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.chooseCategoriesListItem_name);
            CheckBox = itemView.FindViewById<CheckBox>(Resource.Id.chooseCategoriesListItem_checkbox);

            itemView.Click += (sender, e) =>
            {
                CheckBox.Checked = !CheckBox.Checked;
                onClick(CheckBox.Checked, AdapterPosition);
            };

            CheckBox.Click += (sender, e) => { onClick(CheckBox.Checked, AdapterPosition); };
        }
    }
}