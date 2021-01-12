using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.List;

namespace AuthenticatorPro.Fragment
{
    internal class ScanQRCodeBottomSheet : BottomSheet
    {
        public event EventHandler ClickFromCamera;
        public event EventHandler ClickFromGallery;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMenu, container, false);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_action_camera_alt, Resource.String.scanQrCodeFromCamera, ClickFromCamera),
                new(Resource.Drawable.ic_action_image, Resource.String.scanQrCodeFromGallery, ClickFromGallery)
            });

            return view;
        }
    }
}