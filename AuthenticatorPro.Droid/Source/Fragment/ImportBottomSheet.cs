// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.List;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class ImportBottomSheet : BottomSheet
    {
        public event EventHandler ClickGoogleAuthenticator;
        public event EventHandler ClickAuthenticatorPlus;
        public event EventHandler ClickAndOtp;
        public event EventHandler ClickFreeOtpPlus;
        public event EventHandler ClickAegis;
        public event EventHandler ClickBitwarden;
        public event EventHandler ClickWinAuth;
        public event EventHandler ClickAuthy;
        public event EventHandler ClickTotpAuthenticator;
        public event EventHandler ClickSteam;
        public event EventHandler ClickBlizzardAuthenticator;
        public event EventHandler ClickUriList;
        
        public ImportBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.importFrom);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_googleauthenticator, Resource.String.googleAuthenticator, ClickGoogleAuthenticator, Resource.String.viewGuideImportHint),
                new SheetMenuItem(Resource.Drawable.ic_authenticatorplus, Resource.String.authenticatorPlus, ClickAuthenticatorPlus, Resource.String.authenticatorPlusImportHint),
                new SheetMenuItem(Resource.Drawable.ic_andotp, Resource.String.andOtp, ClickAndOtp, Resource.String.andOtpImportHint),
                new SheetMenuItem(Resource.Drawable.ic_freeotpplus, Resource.String.freeOtpPlus, ClickFreeOtpPlus, Resource.String.freeOtpPlusImportHint),
                new SheetMenuItem(Resource.Drawable.ic_aegis, Resource.String.aegis, ClickAegis, Resource.String.aegisImportHint),
                new SheetMenuItem(Shared.Resource.Drawable.auth_bitwarden, Resource.String.bitwarden, ClickBitwarden, Resource.String.bitwardenImportHint),
                new SheetMenuItem(Resource.Drawable.ic_winauth, Resource.String.winAuth, ClickWinAuth, Resource.String.winAuthImportHint),
                new SheetMenuItem(Resource.Drawable.ic_authy, Resource.String.authy, ClickAuthy, Resource.String.viewGuideImportHint),
                new SheetMenuItem(Resource.Drawable.ic_totpauthenticator, Resource.String.totpAuthenticator, ClickTotpAuthenticator, Resource.String.totpAuthenticatorImportHint),
                new SheetMenuItem(Shared.Resource.Drawable.auth_steam, Resource.String.steam, ClickSteam, Resource.String.viewGuideImportHint),
                new SheetMenuItem(Shared.Resource.Drawable.auth_blizzard, Resource.String.blizzardAuthenticator, ClickBlizzardAuthenticator, Resource.String.viewGuideImportHint),
                new SheetMenuItem(Resource.Drawable.ic_list, Resource.String.uriList, ClickUriList, Resource.String.uriListHint)
            });

            return view;
        }
    }
}