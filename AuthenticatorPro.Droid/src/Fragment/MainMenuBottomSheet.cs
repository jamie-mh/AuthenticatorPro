// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Adapter;
using AuthenticatorPro.Shared.View;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class MainMenuBottomSheet : BottomSheet
    {
        public event EventHandler<string> CategoryClicked;
        public event EventHandler BackupClicked;
        public event EventHandler EditCategoriesClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler AboutClicked;

        private readonly ICategoryView _categoryView;
        private CategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        private string _currentCategoryId;

        public MainMenuBottomSheet() : base(Resource.Layout.sheetMainMenu)
        {
            _categoryView = Dependencies.Resolve<ICategoryView>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _categoryListAdapter =
                new CategoriesListAdapter(Activity, _categoryView) { HasStableIds = true };

            _currentCategoryId = Arguments.GetString("currentCategoryId");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.mainMenu, true);

            _categoryList = view.FindViewById<RecyclerView>(Resource.Id.listCategories);
            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetLayoutManager(new LinearLayoutManager(Activity));
            _categoryList.SetItemAnimator(null);

            _categoryListAdapter.NotifyDataSetChanged();

            _categoryListAdapter.CategorySelected += (_, id) =>
            {
                CategoryClicked?.Invoke(this, id);
            };

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.ic_action_backup, Resource.String.backup, BackupClicked),
                    new(Resource.Drawable.ic_action_category, Resource.String.editCategories,
                        EditCategoriesClicked),
                    new(Resource.Drawable.ic_action_settings, Resource.String.settings,
                        SettingsClicked),
                    new(Resource.Drawable.ic_action_info_outline, Resource.String.about, AboutClicked)
                });

            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            await _categoryView.LoadFromPersistence();
            _categoryListAdapter.NotifyDataSetChanged();

            var selectedCategoryPosition =
                _currentCategoryId == null ? 0 : _categoryView.IndexOf(_currentCategoryId) + 1;

            _categoryListAdapter.SelectedPosition = selectedCategoryPosition;
            _categoryListAdapter.NotifyItemChanged(selectedCategoryPosition);
        }
    }
}