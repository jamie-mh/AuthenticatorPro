// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Stratum.Droid.Interface.Fragment
{
    public class AddMenuBottomSheet : BottomSheet
    {
        public AddMenuBottomSheet() : base(Resource.Layout.sheetMenu, Resource.String.add)
        {
        }

        public event EventHandler QrCodeClicked;
        public event EventHandler EnterKeyClicked;
        public event EventHandler RestoreClicked;
        public event EventHandler ImportClicked;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
            [
                new SheetMenuItem(Resource.Drawable.baseline_qr_code_24, Resource.String.scanQrCode, QrCodeClicked),
                new SheetMenuItem(Resource.Drawable.baseline_vpn_key_24, Resource.String.enterKey, EnterKeyClicked),
                new SheetMenuItem(Resource.Drawable.baseline_restore_24, Resource.String.restoreBackup, RestoreClicked),
                new SheetMenuItem(Resource.Drawable.baseline_input_24, Resource.String.importFromOtherApps, ImportClicked)
            ]);

            return view;
        }
    }
}