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
    internal class AddMenuBottomSheet : BottomSheet
    {
        public event EventHandler ClickQrCode;
        public event EventHandler ClickEnterKey;
        public event EventHandler ClickRestore;
        public event EventHandler ClickImport;
        
        public AddMenuBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_action_qr_code, Resource.String.scanQrCode, ClickQrCode),
                new SheetMenuItem(Resource.Drawable.ic_action_vpn_key, Resource.String.enterKey, ClickEnterKey),
                new SheetMenuItem(Resource.Drawable.ic_action_restore, Resource.String.restoreBackup, ClickRestore),
                new SheetMenuItem(Resource.Drawable.ic_action_import, Resource.String.importFromOtherApps, ClickImport)
            });

            return view;
        }
    }
}