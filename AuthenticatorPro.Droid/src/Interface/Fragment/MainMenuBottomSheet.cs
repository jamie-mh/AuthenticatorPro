// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Persistence.View;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    public class MainMenuBottomSheet : BottomSheet
    {
        private readonly ICategoryView _categoryView;
        private CategoryMenuListAdapter _categoryMenuListAdapter;
        private RecyclerView _categoryList;

        private string _currentCategoryId;

        public MainMenuBottomSheet() : base(Resource.Layout.sheetMainMenu, Resource.String.mainMenu)
        {
            _categoryView = Dependencies.Resolve<ICategoryView>();
        }

        public event EventHandler<string> CategoryClicked;
        public event EventHandler BackupClicked;
        public event EventHandler CategoriesClicked;
        public event EventHandler IconPacksClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler AboutClicked;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _categoryMenuListAdapter =
                new CategoryMenuListAdapter(Activity, _categoryView) { HasStableIds = true };

            _currentCategoryId = Arguments.GetString("currentCategoryId");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            _categoryList = view.FindViewById<RecyclerView>(Resource.Id.listCategories);
            _categoryList.SetAdapter(_categoryMenuListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetLayoutManager(new LinearLayoutManager(Activity));
            _categoryList.SetItemAnimator(null);

            _categoryMenuListAdapter.NotifyDataSetChanged();

            _categoryMenuListAdapter.CategorySelected += (_, id) => { CategoryClicked?.Invoke(this, id); };

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
            [
                new SheetMenuItem(Resource.Drawable.baseline_save_24, Resource.String.backup, BackupClicked),
                new SheetMenuItem(Resource.Drawable.baseline_category_24, Resource.String.categories, CategoriesClicked),
                new SheetMenuItem(Resource.Drawable.baseline_folder_24, Resource.String.iconPacks, IconPacksClicked),
                new SheetMenuItem(Resource.Drawable.baseline_settings_24, Resource.String.settings, SettingsClicked),
                new SheetMenuItem(Resource.Drawable.outline_info_24, Resource.String.about, AboutClicked)
            ]);

            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            await _categoryView.LoadFromPersistenceAsync();
            _categoryMenuListAdapter.NotifyDataSetChanged();

            var selectedCategoryPosition =
                _currentCategoryId == null ? 0 : _categoryView.IndexOf(_currentCategoryId) + 1;

            _categoryMenuListAdapter.SelectedPosition = selectedCategoryPosition;
            _categoryMenuListAdapter.NotifyItemChanged(selectedCategoryPosition);
        }
    }
}