// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AuthenticatorMenuBottomSheet : BottomSheet
    {
        public event EventHandler RenameClicked;
        public event EventHandler ChangeIconClicked;
        public event EventHandler AssignCategoriesClicked;
        public event EventHandler ShowQrCodeClicked;
        public event EventHandler DeleteClicked;

        private AuthenticatorType _type;
        private long _counter;

        public AuthenticatorMenuBottomSheet() : base(Resource.Layout.sheetAuthenticatorMenu) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _type = (AuthenticatorType) Arguments.GetInt("type", 0);
            _counter = Arguments.GetLong("counter", 0);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            if (_type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                var counterText = view.FindViewById<TextView>(Resource.Id.textCounter);
                counterText.Text = _counter.ToString();

                view.FindViewById<LinearLayout>(Resource.Id.layoutCounter).Visibility = ViewStates.Visible;
            }

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.ic_edit, Resource.String.rename, RenameClicked),
                    new(Resource.Drawable.ic_image, Resource.String.changeIcon,
                        ChangeIconClicked),
                    new(Resource.Drawable.ic_category, Resource.String.assignCategories,
                        AssignCategoriesClicked),
                    new(Resource.Drawable.ic_qr_code, Resource.String.showQrCode,
                        ShowQrCodeClicked),
                    new(Resource.Drawable.ic_delete, Resource.String.delete, DeleteClicked, null,
                        false, true)
                });

            return view;
        }
    }
}