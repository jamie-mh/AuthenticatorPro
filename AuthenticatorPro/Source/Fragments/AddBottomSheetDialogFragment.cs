using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace AuthenticatorPro.Source.Fragments
{
    public class AddBottomSheetDialogFragment : BottomSheetDialogFragment
    {
        public Action ClickQrCode { get; set; }
        public Action ClickEnterKey { get; set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.addBottomSheetDialog, container, false);

            var scanQrItem = view.FindViewById<LinearLayout>(Resource.Id.addBottomSheetDialog_scanQr);
            var enterKeyItem = view.FindViewById<LinearLayout>(Resource.Id.addBottomSheetDialog_enterKey);

            scanQrItem.Click += (object sender, EventArgs e) => {
                ClickQrCode?.Invoke();
                Dismiss();
            };

            enterKeyItem.Click += (object sender, EventArgs e) => {
                ClickEnterKey?.Invoke();
                Dismiss();
            };

            return view;
        }
    }
}