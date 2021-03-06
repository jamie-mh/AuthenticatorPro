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
    internal class MainMenuBottomSheet : BottomSheet
    {
        public event EventHandler<string> ClickCategory;
        public event EventHandler ClickBackup;
        public event EventHandler ClickManageCategories;
        public event EventHandler ClickSettings;
        public event EventHandler ClickAbout;

        private CategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        private string _currentCategoryId;
        
        public MainMenuBottomSheet() : base(Resource.Layout.sheetMainMenu) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _currentCategoryId = Arguments.GetString("currentCategoryId");

            var categoryIds = Arguments.GetStringArray("categoryIds");
            var categoryNames = Arguments.GetStringArray("categoryNames");
            
            if(categoryIds == null)
                return;
            
            _categoryListAdapter = new CategoriesListAdapter(Activity, categoryIds, categoryNames) {HasStableIds = true};

            var selectedCategoryPosition = _currentCategoryId == null ? 0 : Array.IndexOf(categoryIds, _currentCategoryId) + 1;
            _categoryListAdapter.SelectedPosition = selectedCategoryPosition;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.mainMenu, true);

            _categoryList = view.FindViewById<RecyclerView>(Resource.Id.listCategories);
            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetLayoutManager(new LinearLayoutManager(Activity));

            _categoryListAdapter.NotifyDataSetChanged();

            _categoryListAdapter.CategorySelected += (_, id) =>
            {
                ClickCategory?.Invoke(this, id);
            };

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_action_backup, Resource.String.backup, ClickBackup),
                new SheetMenuItem(Resource.Drawable.ic_action_category, Resource.String.manageCategories, ClickManageCategories),
                new SheetMenuItem(Resource.Drawable.ic_action_settings, Resource.String.settings, ClickSettings),
                new SheetMenuItem(Resource.Drawable.ic_action_info_outline, Resource.String.about, ClickAbout)
            });
            
            return view;
        }
    }
}