// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AddMenuBottomSheet : BottomSheet
    {
        public event EventHandler QrCode;
        public event EventHandler EnterKey;
        public event EventHandler Restore;
        public event EventHandler Import;

        public AddMenuBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new SheetMenuItem(Resource.Drawable.ic_action_qr_code, Resource.String.scanQrCode, QrCode),
                    new SheetMenuItem(Resource.Drawable.ic_action_vpn_key, Resource.String.enterKey, EnterKey),
                    new SheetMenuItem(Resource.Drawable.ic_action_restore, Resource.String.restoreBackup,
                        Restore),
                    new SheetMenuItem(Resource.Drawable.ic_action_import, Resource.String.importFromOtherApps,
                        Import)
                });

            return view;
        }
    }
}