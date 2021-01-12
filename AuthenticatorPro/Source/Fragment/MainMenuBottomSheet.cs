using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.List;

namespace AuthenticatorPro.Fragment
{
    internal class MainMenuBottomSheet : BottomSheet
    {
        public event EventHandler<string> ClickCategory;
        public event EventHandler ClickBackup;
        public event EventHandler ClickManageCategories;
        public event EventHandler ClickSettings;

        private CategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        private readonly string _currCategoryId;
        private readonly CategorySource _source;


        public MainMenuBottomSheet(CategorySource source, string currCategoryId)
        {
            RetainInstance = true;
            _source = source;
            _currCategoryId = currCategoryId;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMainMenu, container, false);
            SetupToolbar(view, Resource.String.mainMenu, true);
            
            _categoryListAdapter = new CategoriesListAdapter(Activity, _source) {HasStableIds = true};

            _categoryList = view.FindViewById<RecyclerView>(Resource.Id.listCategories);
            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetLayoutManager(new LinearLayoutManager(Activity));

            var selectedCategoryPosition = _currCategoryId == null
                ? 0
                : _source.GetPosition(_currCategoryId) + 1;
            
            _categoryListAdapter.SelectedPosition = selectedCategoryPosition;
            _categoryListAdapter.NotifyDataSetChanged();

            _categoryListAdapter.CategorySelected += (_, id) =>
            {
                ClickCategory?.Invoke(this, id);
            };

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_action_backup, Resource.String.backup, ClickBackup),
                new(Resource.Drawable.ic_action_category, Resource.String.manageCategories, ClickManageCategories),
                new(Resource.Drawable.ic_action_settings, Resource.String.settings, ClickSettings)
            });
            
            return view;
        }
    }
}