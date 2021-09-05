// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class ImportBottomSheet : BottomSheet
    {
        public event EventHandler GoogleAuthenticatorClicked;
        public event EventHandler AuthenticatorPlusClicked;
        public event EventHandler AndOtpClicked;
        public event EventHandler FreeOtpPlusClicked;
        public event EventHandler AegisClicked;
        public event EventHandler BitwardenClicked;
        public event EventHandler WinAuthClicked;
        public event EventHandler AuthyClicked;
        public event EventHandler TotpAuthenticatorClicked;
        public event EventHandler SteamClicked;
        public event EventHandler BlizzardAuthenticatorClicked;
        public event EventHandler UriListClicked;

        public ImportBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.importFrom);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new SheetMenuItem(Resource.Drawable.ic_googleauthenticator, Resource.String.googleAuthenticator,
                        GoogleAuthenticatorClicked, Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_authenticatorplus, Resource.String.authenticatorPlus,
                        AuthenticatorPlusClicked, Resource.String.authenticatorPlusImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_andotp, Resource.String.andOtp, AndOtpClicked,
                        Resource.String.andOtpImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_freeotpplus, Resource.String.freeOtpPlus,
                        FreeOtpPlusClicked, Resource.String.freeOtpPlusImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_aegis, Resource.String.aegis, AegisClicked,
                        Resource.String.aegisImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_bitwarden, Resource.String.bitwarden,
                        BitwardenClicked, Resource.String.bitwardenImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_winauth, Resource.String.winAuth, WinAuthClicked,
                        Resource.String.winAuthImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_authy, Resource.String.authy, AuthyClicked,
                        Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_totpauthenticator, Resource.String.totpAuthenticator,
                        TotpAuthenticatorClicked, Resource.String.totpAuthenticatorImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_steam, Resource.String.steam, SteamClicked,
                        Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_blizzard, Resource.String.blizzardAuthenticator,
                        BlizzardAuthenticatorClicked, Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_list, Resource.String.uriList, UriListClicked,
                        Resource.String.uriListHint)
                });

            return view;
        }
    }
}