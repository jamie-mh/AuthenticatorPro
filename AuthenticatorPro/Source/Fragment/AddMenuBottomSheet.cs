using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class AddMenuBottomSheet : BottomSheet
    {
        public event EventHandler ClickQrCode;
        public event EventHandler ClickEnterKey;
        public event EventHandler ClickRestore;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAddMenu, container, false);

            var scanQrItem = view.FindViewById<LinearLayout>(Resource.Id.buttonScanQRCode);
            var enterKeyItem = view.FindViewById<LinearLayout>(Resource.Id.buttonEnterKey);
            var restoreItem = view.FindViewById<LinearLayout>(Resource.Id.buttonRestore);

            scanQrItem.Click += (sender, e) => {
                ClickQrCode?.Invoke(sender, e);
                Dismiss();
            };

            enterKeyItem.Click += (sender, e) => {
                ClickEnterKey?.Invoke(sender, e);
                Dismiss();
            };

            restoreItem.Click += (sender, e) => {
                ClickRestore?.Invoke(sender, e);
                Dismiss();
            };

            return view;
        }
    }
}