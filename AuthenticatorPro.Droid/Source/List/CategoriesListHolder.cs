using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.List
{
    internal class CategoriesListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> Click;
        public TextView Name { get; }


        public CategoriesListHolder(View itemView) : base(itemView)
        {
            Name = itemView.FindViewById<TextView>(Resource.Id.textName);
            itemView.Click += delegate { Click?.Invoke(this, AdapterPosition); };
        }
    }
}