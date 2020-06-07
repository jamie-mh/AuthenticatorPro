using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class CategoryListHolder : RecyclerView.ViewHolder
    {
        public CategoryListHolder(View itemView, Action<int> renameClick, Action<int> deleteClick) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.categoryListItem_name);
            var renameButton = itemView.FindViewById<ImageButton>(Resource.Id.categoryListItem_rename);
            var deleteButton = itemView.FindViewById<ImageButton>(Resource.Id.categoryListItem_delete);

            renameButton.Click += (sender, e) => renameClick(AdapterPosition);
            deleteButton.Click += (sender, e) => deleteClick(AdapterPosition);
        }

        public TextView Name { get; }
    }
}