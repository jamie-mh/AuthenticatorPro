// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.List;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class EditCategoryMenuBottomSheet: BottomSheet
    {
        public event EventHandler<int> ClickRename;
        public event EventHandler<int> ClickSetDefault;
        public event EventHandler<int> ClickDelete;

        private int _position;
        private bool _isDefault;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _position = Arguments.GetInt("position", -1);
            _isDefault = Arguments.GetBoolean("isDefault", false);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMenu, container, false);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_action_edit, Resource.String.rename, delegate
                {
                    ClickRename(this, _position);
                }),
                new SheetMenuItem(Resource.Drawable.ic_action_star, _isDefault ? Resource.String.clearDefault : Resource.String.setAsDefault, delegate
                {
                    ClickSetDefault(this, _position);
                }),
                new SheetMenuItem(Resource.Drawable.ic_action_delete, Resource.String.delete, delegate
                {
                    ClickDelete(this, _position);
                }, null, true)
            });

            return view;
        }
    }
}