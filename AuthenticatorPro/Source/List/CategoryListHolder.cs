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
            RenameButton = itemView.FindViewById<ImageButton>(Resource.Id.categoryListItem_rename);
            DeleteButton = itemView.FindViewById<ImageButton>(Resource.Id.categoryListItem_delete);

            RenameButton.Click += (sender, e) => renameClick(AdapterPosition);
            DeleteButton.Click += (sender, e) => deleteClick(AdapterPosition);
        }

        public TextView Name { get; }
        public ImageButton RenameButton { get; }
        public ImageButton DeleteButton { get; }
    }
}