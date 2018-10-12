using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace ProAuth.Utilities
{
    class AuthHolder : RecyclerView.ViewHolder
    {
        public TextView Issuer { get; set; }
        public TextView Username { get; }
        public TextView Code { get; set; }
        public TextView Timer { get; set; }
        public TextView Counter { get; set; }
        public ImageButton RefreshButton { get; set; }

        public AuthHolder(View itemView, Action<int> clickListener, Action<int> optionsClickListener, Action<int> refreshClickListener) : base(itemView)
        {
            Issuer = itemView.FindViewById<TextView>(Resource.Id.authListItem_issuer);
            Username = itemView.FindViewById<TextView>(Resource.Id.authListItem_username);
            Code = itemView.FindViewById<TextView>(Resource.Id.authListItem_code);
            Timer = itemView.FindViewById<TextView>(Resource.Id.authListItem_timer);
            Counter = itemView.FindViewById<TextView>(Resource.Id.authListItem_counter);
            RefreshButton = itemView.FindViewById<ImageButton>(Resource.Id.authListItem_refresh);

            ImageButton optionsButton = itemView.FindViewById<ImageButton>(Resource.Id.authListItem_options);

            itemView.Click += (sender, e) => clickListener(AdapterPosition);
            optionsButton.Click += (sender, e) => optionsClickListener(AdapterPosition);
            RefreshButton.Click += (sender, e) => refreshClickListener(AdapterPosition);
        }
    }
}