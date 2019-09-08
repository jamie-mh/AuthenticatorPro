using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.FilesystemList
{
    internal class FilesystemHolder : RecyclerView.ViewHolder
    {
        public FilesystemHolder(View itemView, Action<int> clickListener) : base(itemView)
        {
            Icon = itemView.FindViewById<ImageView>(Resource.Id.fileListItem_icon);
            Name = itemView.FindViewById<TextView>(Resource.Id.fileListItem_name);

            itemView.Click += (sender, e) => clickListener(AdapterPosition);
        }

        public ImageView Icon { get; set; }
        public TextView Name { get; set; }
    }
}