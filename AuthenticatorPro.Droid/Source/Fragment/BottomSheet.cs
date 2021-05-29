// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.List;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Internal;
using Java.Lang;

namespace AuthenticatorPro.Droid.Fragment
{
    internal abstract class BottomSheet : BottomSheetDialogFragment
    {
        private const int MaxWidth = 650;
        private readonly int _layout;

        protected BottomSheet(int layout)
        {
            _layout = layout;
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = (BottomSheetDialog) base.OnCreateDialog(savedInstanceState);
            dialog.ShowEvent += delegate
            {
                var bottomSheet = dialog.FindViewById<FrameLayout>(Resource.Id.design_bottom_sheet);
                BottomSheetBehavior.From(bottomSheet).State = BottomSheetBehavior.StateExpanded;
            };

            return dialog;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            SetStyle(StyleNormal, Resource.Style.BottomSheetStyle);
            var prefs = new PreferenceWrapper(Context);
            Context.Theme.ApplyStyle(prefs.AccentOverlay, true);
            return inflater.Inflate(_layout, container, false);
        }

        public override void Show(FragmentManager manager, string tag)
        {
            try
            {
                var transaction = manager.BeginTransaction();
                transaction.Add(this, Tag);
                transaction.CommitAllowingStateLoss();
            }
            catch(IllegalStateException e)
            {
                // This sometimes fails for some reason, not sure why
                Logger.Error(e);
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            if(Activity.Resources.Configuration.ScreenWidthDp > MaxWidth)
                Dialog.Window.SetLayout((int) ViewUtils.DpToPx(Activity, MaxWidth), -1);
        }

        protected void SetupToolbar(View view, int titleRes, bool showCloseButton = false)
        {
            var toolbar = view.FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            toolbar.SetTitle(titleRes);
            toolbar.Visibility = ViewStates.Visible;

            if(!showCloseButton)
                return;
            
            toolbar.InflateMenu(Resource.Menu.sheet);
            toolbar.MenuItemClick += (_, args) =>
            {
                if(args.Item.ItemId == Resource.Id.actionClose)
                    Dismiss();
            };
        }

        protected void SetupMenu(RecyclerView list, List<SheetMenuItem> items)
        {
            var adapter = new SheetMenuAdapter(Context, items);
            adapter.ItemClick += delegate
            {
                Dismiss();
            };
            
            list.SetAdapter(adapter);
            list.HasFixedSize = true;

            var layout = new LinearLayoutManager(Context);
            list.SetLayoutManager(layout);
        }

        protected void SetCancelable(bool cancelable)
        {
            Dialog.SetCancelable(cancelable);
            Dialog.SetCanceledOnTouchOutside(cancelable);
        }
    }
}