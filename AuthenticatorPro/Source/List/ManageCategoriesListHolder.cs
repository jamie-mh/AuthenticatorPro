using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class ManageCategoriesListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> MenuClick;
        public TextView Name { get; }


        public ManageCategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);

            var menuButton = itemView.FindViewById<ImageButton>(Resource.Id.buttonMenu);
            menuButton.Click += (sender, args) =>
            {
                MenuClick?.Invoke(this, AdapterPosition);
            };
        }
    }
}