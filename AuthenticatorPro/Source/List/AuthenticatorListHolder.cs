using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> Click;
        public event EventHandler<int> OptionsClick;
        public event EventHandler<int> RefreshClick;

        public TextView Issuer { get; }
        public TextView Username { get; }
        public TextView Code { get; }
        public ProgressBar ProgressBar { get; }
        public TextView Counter { get; }
        public ImageButton RefreshButton { get; }
        public ImageView Icon { get; }


        public AuthenticatorListHolder(View view) : base(view)
        {
            Issuer = view.FindViewById<TextView>(Resource.Id.authListItem_issuer);
            Username = view.FindViewById<TextView>(Resource.Id.authListItem_username);
            Code = view.FindViewById<TextView>(Resource.Id.authListItem_code);
            ProgressBar = view.FindViewById<ProgressBar>(Resource.Id.authList_progress);
            Counter = view.FindViewById<TextView>(Resource.Id.authListItem_counter);
            RefreshButton = view.FindViewById<ImageButton>(Resource.Id.authListItem_refresh);
            Icon = view.FindViewById<ImageView>(Resource.Id.authListItem_icon);

            var optionsButton = view.FindViewById<ImageButton>(Resource.Id.authListItem_options);

            view.Click += (sender, e) => Click?.Invoke(this, AdapterPosition);
            optionsButton.Click += (sender, e) => OptionsClick?.Invoke(this, AdapterPosition);
            RefreshButton.Click += (sender, e) => RefreshClick?.Invoke(this, AdapterPosition);
        }
    }
}