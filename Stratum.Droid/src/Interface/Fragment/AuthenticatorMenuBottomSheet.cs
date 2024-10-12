// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Stratum.Core;
using Stratum.Core.Generator;
using Google.Android.Material.TextView;

namespace Stratum.Droid.Interface.Fragment
{
    public class AuthenticatorMenuBottomSheet : BottomSheet
    {
        private AuthenticatorType _type;
        private long _counter;
        private long _copyCount;

        public AuthenticatorMenuBottomSheet() : base(Resource.Layout.sheetAuthenticatorMenu, Resource.String.edit)
        {
        }

        public event EventHandler EditClicked;
        public event EventHandler ChangeIconClicked;
        public event EventHandler AssignCategoriesClicked;
        public event EventHandler ShowQrCodeClicked;
        public event EventHandler DeleteClicked;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _type = (AuthenticatorType) Arguments.GetInt("type", 0);
            _counter = Arguments.GetLong("counter", 0);
            _copyCount = Arguments.GetInt("copyCount", 0);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            if (_type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                var counterText = view.FindViewById<MaterialTextView>(Resource.Id.textCounter);
                counterText.Text = _counter.ToString();

                view.FindViewById<LinearLayout>(Resource.Id.layoutCounter).Visibility = ViewStates.Visible;
            }

            var preferences = new PreferenceWrapper(RequireContext());

            if (preferences.SortMode is SortMode.CopyCountAscending or SortMode.CopyCountDescending)
            {
                var copyCountText = view.FindViewById<MaterialTextView>(Resource.Id.textCopyCount);
                copyCountText.Text = _copyCount.ToString();

                view.FindViewById<LinearLayout>(Resource.Id.layoutCopyCount).Visibility = ViewStates.Visible;
            }

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
            [
                new SheetMenuItem(Resource.Drawable.baseline_edit_24, Resource.String.editDetails, EditClicked),
                new SheetMenuItem(Resource.Drawable.baseline_image_24, Resource.String.changeIcon,
                    ChangeIconClicked),
                new SheetMenuItem(Resource.Drawable.baseline_category_24, Resource.String.assignCategories,
                    AssignCategoriesClicked),
                new SheetMenuItem(Resource.Drawable.baseline_qr_code_24, Resource.String.showQrCode,
                    ShowQrCodeClicked),
                new SheetMenuItem(Resource.Drawable.baseline_delete_24, Resource.String.delete, DeleteClicked, null,
                    true)
            ]);

            return view;
        }
    }
}