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
        public event EventHandler GoogleAuthenticator;
        public event EventHandler AuthenticatorPlus;
        public event EventHandler AndOtp;
        public event EventHandler FreeOtpPlus;
        public event EventHandler Aegis;
        public event EventHandler Bitwarden;
        public event EventHandler WinAuth;
        public event EventHandler Authy;
        public event EventHandler TotpAuthenticator;
        public event EventHandler Steam;
        public event EventHandler BlizzardAuthenticator;
        public event EventHandler UriList;

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
                        GoogleAuthenticator, Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_authenticatorplus, Resource.String.authenticatorPlus,
                        AuthenticatorPlus, Resource.String.authenticatorPlusImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_andotp, Resource.String.andOtp, AndOtp,
                        Resource.String.andOtpImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_freeotpplus, Resource.String.freeOtpPlus,
                        FreeOtpPlus, Resource.String.freeOtpPlusImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_aegis, Resource.String.aegis, Aegis,
                        Resource.String.aegisImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_bitwarden, Resource.String.bitwarden,
                        Bitwarden, Resource.String.bitwardenImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_winauth, Resource.String.winAuth, WinAuth,
                        Resource.String.winAuthImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_authy, Resource.String.authy, Authy,
                        Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_totpauthenticator, Resource.String.totpAuthenticator,
                        TotpAuthenticator, Resource.String.totpAuthenticatorImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_steam, Resource.String.steam, Steam,
                        Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Shared.Resource.Drawable.auth_blizzard, Resource.String.blizzardAuthenticator,
                        BlizzardAuthenticator, Resource.String.viewGuideImportHint),
                    new SheetMenuItem(Resource.Drawable.ic_list, Resource.String.uriList, UriList,
                        Resource.String.uriListHint)
                });

            return view;
        }
    }
}