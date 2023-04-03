// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class EditCategoryMenuBottomSheet : BottomSheet
    {
        public event EventHandler<int> RenameClicked;
        public event EventHandler<int> SetDefaultClicked;
        public event EventHandler<int> DeleteClicked;

        private int _position;
        private bool _isDefault;

        public EditCategoryMenuBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _position = Arguments.GetInt("position", -1);
            _isDefault = Arguments.GetBoolean("isDefault", false);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.baseline_edit_24, Resource.String.rename, delegate
                {
                    RenameClicked(this, _position);
                }),
                new(Resource.Drawable.baseline_star_24,
                    _isDefault ? Resource.String.clearDefault : Resource.String.setAsDefault, delegate
                    {
                        SetDefaultClicked(this, _position);
                    }),
                new(Resource.Drawable.baseline_delete_24, Resource.String.delete, delegate
                {
                    DeleteClicked(this, _position);
                }, null, true)
            });

            return view;
        }
    }
}