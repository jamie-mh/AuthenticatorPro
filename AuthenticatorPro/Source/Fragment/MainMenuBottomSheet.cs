using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.List;

namespace AuthenticatorPro.Fragment
{
    internal class MainMenuBottomSheet : BottomSheet
    {
        public event EventHandler<string> CategoryClick;
        public event EventHandler BackupClick;
        public event EventHandler ManageCategoriesClick;
        public event EventHandler SettingsClick;

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

            _categoryListAdapter.CategorySelected += (sender, id) =>
            {
                CategoryClick?.Invoke(this, id);
            };

            var backupButton = view.FindViewById<LinearLayout>(Resource.Id.buttonBackup);
            var manageCategoriesButton = view.FindViewById<LinearLayout>(Resource.Id.buttonManageCategories);
            var settingsButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSettings);

            backupButton.Click += (sender, args) =>
            {
                BackupClick?.Invoke(this, null);
                Dismiss();
            };

            manageCategoriesButton.Click += (sender, args) =>
            {
                ManageCategoriesClick?.Invoke(this, null);
                Dismiss();
            };

            settingsButton.Click += (sender, args) =>
            {
                SettingsClick?.Invoke(this, null);
                Dismiss();
            };
            return view;
        }
    }
}