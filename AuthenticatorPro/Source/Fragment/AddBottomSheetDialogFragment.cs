using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace AuthenticatorPro.Fragment
{
    internal class AddBottomSheetDialogFragment : BottomSheetDialogFragment
    {
        public event EventHandler ClickQrCode;
        public event EventHandler ClickEnterKey;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.addBottomSheetDialog, container, false);

            var scanQrItem = view.FindViewById<LinearLayout>(Resource.Id.addBottomSheetDialog_scanQr);
            var enterKeyItem = view.FindViewById<LinearLayout>(Resource.Id.addBottomSheetDialog_enterKey);

            scanQrItem.Click += (sender, e) => {
                ClickQrCode?.Invoke(sender, e);
                Dismiss();
            };

            enterKeyItem.Click += (sender, e) => {
                ClickEnterKey?.Invoke(sender, e);
                Dismiss();
            };

            return view;
        }
    }
}