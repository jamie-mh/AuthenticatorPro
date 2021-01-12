using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.List;

namespace AuthenticatorPro.Fragment
{
    internal class ImportBottomSheet : BottomSheet
    {
        public event EventHandler ClickGoogleAuthenticator;
        public event EventHandler ClickAuthenticatorPlus;
        public event EventHandler ClickAndOtp;
        public event EventHandler ClickFreeOtpPlus;
        public event EventHandler ClickAegis;
        public event EventHandler ClickWinAuth;
        public event EventHandler ClickTotpAuthenticator;
        public event EventHandler ClickSteam;
        public event EventHandler ClickBlizzardAuthenticator;
        

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMenu, container, false);
            SetupToolbar(view, Resource.String.importFrom);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_googleauthenticator, Resource.String.googleAuthenticator, ClickGoogleAuthenticator),
                new(Resource.Drawable.ic_authenticatorplus, Resource.String.authenticatorPlus, ClickAuthenticatorPlus),
                new(Resource.Drawable.ic_andotp, Resource.String.andOtp, ClickAndOtp),
                new(Resource.Drawable.ic_freeotpplus, Resource.String.freeOtpPlus, ClickFreeOtpPlus),
                new(Resource.Drawable.ic_aegis, Resource.String.aegis, ClickAegis),
                new(Resource.Drawable.ic_winauth, Resource.String.winAuth, ClickWinAuth),
                new(Resource.Drawable.ic_totpauthenticator, Resource.String.totpAuthenticator, ClickTotpAuthenticator),
                new(Resource.Drawable.auth_steam, Resource.String.steam, ClickSteam),
                new(Resource.Drawable.auth_blizzard, Resource.String.blizzardAuthenticator, ClickBlizzardAuthenticator)
            });

            return view;
        }
    }
}