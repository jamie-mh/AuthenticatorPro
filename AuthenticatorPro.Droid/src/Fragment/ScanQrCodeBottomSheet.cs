// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class ScanQrCodeBottomSheet : BottomSheet
    {
        public event EventHandler FromCameraClicked;
        public event EventHandler FromGalleryClicked;

        public ScanQrCodeBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.ic_action_camera_alt, Resource.String.scanQrCodeFromCamera,
                        FromCameraClicked),
                    new(Resource.Drawable.ic_action_image, Resource.String.scanQrCodeFromGallery,
                        FromGalleryClicked)
                });

            return view;
        }
    }
}