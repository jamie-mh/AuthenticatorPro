// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Interface.Fragment
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
                    new(Resource.Drawable.baseline_photo_camera_24, Resource.String.scanQrCodeFromCamera,
                        FromCameraClicked),
                    new(Resource.Drawable.baseline_image_24, Resource.String.scanQrCodeFromGallery,
                        FromGalleryClicked)
                });

            return view;
        }
    }
}