using Android.OS;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Internal;
using Orientation = Android.Content.Res.Orientation;

namespace AuthenticatorPro.Fragment
{
    internal class BottomSheet : BottomSheetDialogFragment
    {
        private const int MaxWidth = 650;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetStyle(StyleNormal, Resource.Style.BottomSheetStyle);
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

        public override void OnResume()
        {
            base.OnResume();

            if(Activity.Resources.Configuration.ScreenWidthDp > MaxWidth)
                Dialog.Window.SetLayout((int) ViewUtils.DpToPx(Activity, MaxWidth), -1);
        }
    }
}