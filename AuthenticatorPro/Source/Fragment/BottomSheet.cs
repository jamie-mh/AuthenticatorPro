using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Internal;

namespace AuthenticatorPro.Fragment
{
    internal abstract class BottomSheet : BottomSheetDialogFragment
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

        protected void SetupToolbar(View view, int titleRes)
        {
            var toolbar = view.FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            toolbar.SetTitle(titleRes);
            toolbar.InflateMenu(Resource.Menu.sheet);
            toolbar.MenuItemClick += (sender, args) =>
            {
                if(args.Item.ItemId == Resource.Id.actionClose)
                    Dismiss();
            };
        }
    }
}