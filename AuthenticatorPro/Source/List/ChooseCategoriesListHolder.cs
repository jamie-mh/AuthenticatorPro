using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class ChooseCategoriesListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> ItemClick;

        public TextView Name { get; }
        public CheckBox CheckBox { get; }


        public ChooseCategoriesListHolder(View view) : base(view)
        {
            Name = view.FindViewById<TextView>(Resource.Id.chooseCategoriesListItem_name);
            CheckBox = view.FindViewById<CheckBox>(Resource.Id.chooseCategoriesListItem_checkbox);

            view.Click += (sender, e) =>
            {
                CheckBox.Checked = !CheckBox.Checked;
                ItemClick?.Invoke(this, AdapterPosition);
            };

            CheckBox.Click += (sender, e) =>
            {
                ItemClick?.Invoke(this, AdapterPosition);
            };
        }
    }
}