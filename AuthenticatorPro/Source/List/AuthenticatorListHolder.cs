using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.List
{
    internal class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public TextView Issuer { get; }
        public TextView Username { get; }
        public TextView Code { get; }
        public ProgressBar ProgressBar { get; }
        public ImageButton MenuButton { get; }
        public ImageButton RefreshButton { get; }
        public ImageView Icon { get; }


        public AuthenticatorListHolder(View view) : base(view)
        {
            Issuer = view.FindViewById<TextView>(Resource.Id.textIssuer);
            Username = view.FindViewById<TextView>(Resource.Id.textUsername);
            Code = view.FindViewById<TextView>(Resource.Id.textCode);
            ProgressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
            MenuButton = view.FindViewById<ImageButton>(Resource.Id.buttonMenu);
            RefreshButton = view.FindViewById<ImageButton>(Resource.Id.buttonRefresh);
            Icon = view.FindViewById<ImageView>(Resource.Id.imageIcon);
        }
    }
}