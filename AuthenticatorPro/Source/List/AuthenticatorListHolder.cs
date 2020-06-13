using System;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public event EventHandler<int> Click;
        public event EventHandler<int> MenuClick;
        public event EventHandler<int> RefreshClick;

        public TextView Issuer { get; }
        public TextView Username { get; }
        public TextView Code { get; }
        public ProgressBar ProgressBar { get; }
        public ImageButton RefreshButton { get; }
        public ImageView Icon { get; }


        public AuthenticatorListHolder(View view) : base(view)
        {
            Issuer = view.FindViewById<TextView>(Resource.Id.textIssuer);
            Username = view.FindViewById<TextView>(Resource.Id.textUsername);
            Code = view.FindViewById<TextView>(Resource.Id.textCode);
            ProgressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
            RefreshButton = view.FindViewById<ImageButton>(Resource.Id.buttonRefresh);
            Icon = view.FindViewById<ImageView>(Resource.Id.imageIcon);

            var menuButton = view.FindViewById<ImageButton>(Resource.Id.buttonMenu);

            view.Click += (sender, e) => Click?.Invoke(this, AdapterPosition);
            menuButton.Click += (sender, e) => MenuClick?.Invoke(this, AdapterPosition);
            RefreshButton.Click += (sender, e) => RefreshClick?.Invoke(this, AdapterPosition);
        }
    }
}