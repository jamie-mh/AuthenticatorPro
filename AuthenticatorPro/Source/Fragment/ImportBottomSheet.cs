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
                new(Resource.Drawable.ic_googleauthenticator, Resource.String.googleAuthenticator, ClickGoogleAuthenticator, Resource.String.viewGuideImportHint),
                new(Resource.Drawable.ic_authenticatorplus, Resource.String.authenticatorPlus, ClickAuthenticatorPlus, Resource.String.authenticatorPlusImportHint),
                new(Resource.Drawable.ic_andotp, Resource.String.andOtp, ClickAndOtp, Resource.String.andOtpImportHint),
                new(Resource.Drawable.ic_freeotpplus, Resource.String.freeOtpPlus, ClickFreeOtpPlus, Resource.String.freeOtpPlusImportHint),
                new(Resource.Drawable.ic_aegis, Resource.String.aegis, ClickAegis, Resource.String.aegisImportHint),
                new(Resource.Drawable.ic_winauth, Resource.String.winAuth, ClickWinAuth, Resource.String.winAuthImportHint),
                new(Resource.Drawable.ic_totpauthenticator, Resource.String.totpAuthenticator, ClickTotpAuthenticator, Resource.String.totpAuthenticatorImportHint),
                new(Resource.Drawable.auth_steam, Resource.String.steam, ClickSteam, Resource.String.viewGuideImportHint),
                new(Resource.Drawable.auth_blizzard, Resource.String.blizzardAuthenticator, ClickBlizzardAuthenticator, Resource.String.viewGuideImportHint)
            });

            return view;
        }
    }
}