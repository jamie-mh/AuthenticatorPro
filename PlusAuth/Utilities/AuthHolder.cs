using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace PlusAuth.Utilities
{
    internal class AuthHolder : RecyclerView.ViewHolder
    {
        public TextView Issuer { get; set; }
        public TextView Username { get; }
        public TextView Code { get; set; }
        public ProgressBar ProgressBar { get; set; }
        public TextView Counter { get; set; }
        public ImageButton RefreshButton { get; set; }
        public ImageView Icon { get; set; }

        public AuthHolder(View itemView, Action<int> clickListener, Action<int> optionsClickListener, Action<int> refreshClickListener) : base(itemView)
        {
            Issuer = itemView.FindViewById<TextView>(Resource.Id.authListItem_issuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.authListItem_username);
            Code = itemView.FindViewById<TextView>(Resource.Id.authListItem_code);
            ProgressBar = itemView.FindViewById<ProgressBar>(Resource.Id.authList_progress);
            Counter = itemView.FindViewById<TextView>(Resource.Id.authListItem_counter);
            RefreshButton = itemView.FindViewById<ImageButton>(Resource.Id.authListItem_refresh);
            Icon = itemView.FindViewById<ImageView>(Resource.Id.authListItem_icon);

            ImageButton optionsButton = itemView.FindViewById<ImageButton>(Resource.Id.authListItem_options);

            itemView.Click += (sender, e) => clickListener(AdapterPosition);
            optionsButton.Click += (sender, e) => optionsClickListener(AdapterPosition);
            RefreshButton.Click += (sender, e) => refreshClickListener(AdapterPosition);
        }
    }
}