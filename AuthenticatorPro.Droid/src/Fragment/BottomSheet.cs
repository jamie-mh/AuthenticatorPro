// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Adapter;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.AppBar;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Internal;
using Java.Lang;
using System;
using System.Collections.Generic;
using ContextThemeWrapper = AndroidX.AppCompat.View.ContextThemeWrapper;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace AuthenticatorPro.Droid.Fragment
{
    internal abstract class BottomSheet : BottomSheetDialogFragment
    {
        public event EventHandler Dismissed;

        private const int MaxWidth = 600;

        private readonly int _layout;
        protected LayoutInflater StyledInflater;

        protected BottomSheet(int layout)
        {
            _layout = layout;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = (BottomSheetDialog) base.OnCreateDialog(savedInstanceState);
            dialog.ShowEvent += delegate
            {
                var bottomSheet = dialog.FindViewById<FrameLayout>(Resource.Id.design_bottom_sheet);
                BottomSheetBehavior.From(bottomSheet).State = BottomSheetBehavior.StateExpanded;
            };

            if (Build.VERSION.SdkInt < BuildVersionCodes.OMr1)
            {
                dialog.Window.SetNavigationBarColor(Color.Black);
            }

            return dialog;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        public override View OnCreateView(LayoutInflater contextInflater, ViewGroup container,
            Bundle savedInstanceState)
        {
            var contextThemeWrapper = new ContextThemeWrapper(Activity, Resource.Style.BottomSheetStyle);
            var prefs = new PreferenceWrapper(Context);
            contextThemeWrapper.Theme.ApplyStyle(AccentColourMap.GetOverlayId(prefs.AccentColour), true);
            StyledInflater = contextInflater.CloneInContext(contextThemeWrapper);

            return StyledInflater.Inflate(_layout, container, false);
        }

        public override void Show(FragmentManager manager, string tag)
        {
            try
            {
                var transaction = manager.BeginTransaction();
                transaction.Add(this, Tag);
                transaction.CommitAllowingStateLoss();
            }
            catch (IllegalStateException e)
            {
                // This sometimes fails for some reason, not sure why
                Logger.Error(e);
            }
        }

        public override void OnResume()
        {
            base.OnResume();

            if (Activity.Resources.Configuration.ScreenWidthDp > MaxWidth)
            {
                Dialog.Window.SetLayout((int) ViewUtils.DpToPx(Activity, MaxWidth), -1);
            }
        }

        protected void SetupToolbar(View view, int titleRes, bool showCloseButton = false)
        {
            var toolbar = view.FindViewById<MaterialToolbar>(Resource.Id.toolbar);
            toolbar.SetTitle(titleRes);
            toolbar.Visibility = ViewStates.Visible;

            if (!showCloseButton)
            {
                return;
            }

            toolbar.InflateMenu(Resource.Menu.sheet);
            toolbar.MenuItemClick += (_, args) =>
            {
                if (args.Item.ItemId == Resource.Id.actionClose)
                {
                    Dismiss();
                }
            };
        }

        protected void SetupMenu(RecyclerView list, List<SheetMenuItem> items)
        {
            var adapter = new SheetMenuAdapter(Context, items);
            adapter.ItemClicked += delegate
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