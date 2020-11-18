using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class ScanQRCodeBottomSheet : BottomSheet
    {
        public event EventHandler ClickFromCamera;
        public event EventHandler ClickFromGallery;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetScanQRCodeMenu, container, false);

            var fromCameraItem = view.FindViewById<LinearLayout>(Resource.Id.buttonScanFromCamera);
            var fromGalleryItem = view.FindViewById<LinearLayout>(Resource.Id.buttonScanFromGallery);

            fromCameraItem.Click += (sender, e) => {
                ClickFromCamera?.Invoke(sender, e);
                Dismiss();
            };

            fromGalleryItem.Click += (sender, e) => {
                ClickFromGallery?.Invoke(sender, e);
                Dismiss();
            };

            return view;
        }
    }
}