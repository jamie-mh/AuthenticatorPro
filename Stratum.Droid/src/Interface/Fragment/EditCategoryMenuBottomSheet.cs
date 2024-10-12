// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace Stratum.Droid.Interface.Fragment
{
    public class EditCategoryMenuBottomSheet : BottomSheet
    {
        private string _id;
        private bool _isDefault;

        public EditCategoryMenuBottomSheet() : base(Resource.Layout.sheetMenu, Resource.String.edit)
        {
        }

        public event EventHandler<string> RenameClicked;
        public event EventHandler<string> AssignEntriesClicked;
        public event EventHandler<string> SetDefaultClicked;
        public event EventHandler<string> DeleteClicked;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _id = Arguments.GetString("id");
            _isDefault = Arguments.GetBoolean("isDefault", false);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, [
                new SheetMenuItem(Resource.Drawable.baseline_edit_24, Resource.String.rename, delegate { RenameClicked(this, _id); }),
                new SheetMenuItem(Resource.Drawable.baseline_checklist_24, Resource.String.assignEntries,
                    delegate { AssignEntriesClicked(this, _id); }),
                new SheetMenuItem(Resource.Drawable.baseline_star_24,
                    _isDefault ? Resource.String.clearDefault : Resource.String.setAsDefault,
                    delegate { SetDefaultClicked(this, _id); }),
                new SheetMenuItem(Resource.Drawable.baseline_delete_24, Resource.String.delete,
                    delegate { DeleteClicked(this, _id); }, null, true)
            ]);

            return view;
        }
    }
}