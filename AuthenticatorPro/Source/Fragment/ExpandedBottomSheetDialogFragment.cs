using Android.OS;
using Android.Widget;
using Google.Android.Material.BottomSheet;

namespace AuthenticatorPro.Fragment
{
    internal class ExpandedBottomSheetDialogFragment : BottomSheetDialogFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetStyle(StyleNormal, Resource.Style.ExpandedBottomSheetDialogStyle);
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = (BottomSheetDialog) base.OnCreateDialog(savedInstanceState);
            dialog.ShowEvent += (sender, e) =>
            {
                var bottomSheet = dialog.FindViewById<FrameLayout>(Resource.Id.design_bottom_sheet);
                BottomSheetBehavior.From(bottomSheet).State = BottomSheetBehavior.StateExpanded;
            };

            return dialog;
        }
    }
}