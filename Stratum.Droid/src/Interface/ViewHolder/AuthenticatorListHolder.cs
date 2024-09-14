// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.ViewHolder
{
    public class AuthenticatorListHolder : RecyclerView.ViewHolder
    {
        public AuthenticatorListHolder(View view) : base(view)
        {
            Issuer = view.FindViewById<MaterialTextView>(Resource.Id.textIssuer);
            Username = view.FindViewById<MaterialTextView>(Resource.Id.textUsername);
            Code = view.FindViewById<MaterialTextView>(Resource.Id.textCode);
            ProgressIndicator = view.FindViewById<LinearProgressIndicator>(Resource.Id.progressIndicator);
            MenuButton = view.FindViewById<ImageButton>(Resource.Id.buttonMenu);
            RefreshButton = view.FindViewById<ImageButton>(Resource.Id.buttonRefresh);
            Icon = view.FindViewById<ImageView>(Resource.Id.imageIcon);
        }

        public MaterialTextView Issuer { get; }
        public MaterialTextView Username { get; }
        public MaterialTextView Code { get; }
        public LinearProgressIndicator ProgressIndicator { get; }
        public ImageButton MenuButton { get; }
        public ImageButton RefreshButton { get; }
        public ImageView Icon { get; }
    }
}