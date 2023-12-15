// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Interface.Adapter;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Internal;
using Google.Android.Material.TextView;
using Java.Lang;
using Serilog;
using FragmentManager = AndroidX.Fragment.App.FragmentManager;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    public abstract class BottomSheet : BottomSheetDialogFragment
    {
        private const int MaxWidth = 600;

        private readonly ILogger _log = Log.ForContext<BottomSheet>();
        private readonly int _layout;
        private readonly int _title;

        protected BottomSheet(int layout, int title)
        {
            _layout = layout;
            _title = title;
        }

        protected bool IsDark { get; private set; }
        public event EventHandler Dismissed;

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var dialog = (BottomSheetDialog) base.OnCreateDialog(savedInstanceState);
            dialog.ShowEvent += delegate
            {
                var bottomSheet = dialog.FindViewById<FrameLayout>(Resource.Id.design_bottom_sheet);
                BottomSheetBehavior.From(bottomSheet).State = BottomSheetBehavior.StateExpanded;
            };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N)
            {
                dialog.Window.SetNavigationBarColor(Color.Transparent);
            }
            else if (Build.VERSION.SdkInt < BuildVersionCodes.OMr1)
            {
                dialog.Window.SetNavigationBarColor(Color.Black);
            }

            dialog.Window.SetSoftInputMode(SoftInput.AdjustResize);

            var preferences = new PreferenceWrapper(Context);
            if (!preferences.AllowScreenshots)
            {
                dialog.Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
            }

            var baseActivity = (BaseActivity) RequireActivity();
            IsDark = baseActivity.IsDark;

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
            var view = contextInflater.Inflate(_layout, container, false);

            var title = view.FindViewById<MaterialTextView>(Resource.Id.textTitle);
            title.SetText(_title);

            return view;
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
                _log.Error(e, "Illegal state in showing fragment");
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

        protected void SetupMenu(RecyclerView list, List<SheetMenuItem> items)
        {
            var adapter = new SheetMenuAdapter(items);
            adapter.ItemClicked += delegate { Dismiss(); };

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