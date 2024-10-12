// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Stratum.Droid.Interface.Fragment
{
    public class ScanQrCodeBottomSheet : BottomSheet
    {
        public ScanQrCodeBottomSheet() : base(Resource.Layout.sheetMenu, Resource.String.scanQrCode)
        {
        }

        public event EventHandler FromCameraClicked;
        public event EventHandler FromGalleryClicked;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
            [
                new SheetMenuItem(Resource.Drawable.baseline_photo_camera_24, Resource.String.scanQrCodeFromCamera,
                    FromCameraClicked),
                new SheetMenuItem(Resource.Drawable.baseline_image_24, Resource.String.scanQrCodeFromGallery,
                    FromGalleryClicked)
            ]);

            return view;
        }
    }
}