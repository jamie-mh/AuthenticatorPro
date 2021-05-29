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
    internal class ScanQRCodeBottomSheet : BottomSheet
    {
        public event EventHandler ClickFromCamera;
        public event EventHandler ClickFromGallery;
        
        public ScanQRCodeBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_action_camera_alt, Resource.String.scanQrCodeFromCamera, ClickFromCamera),
                new SheetMenuItem(Resource.Drawable.ic_action_image, Resource.String.scanQrCodeFromGallery, ClickFromGallery)
            });

            return view;
        }
    }
}