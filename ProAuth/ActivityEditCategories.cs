using System;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget;
using ProAuth.Data;
using ProAuth.Utilities;
using SQLite;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using AlertDialog = Android.Support.V7.App.AlertDialog;

namespace ProAuth
{
    [Activity(Label = "EditCategoriesActivity")]
    public class ActivityEditCategories: AppCompatActivity
    {
        private SQLiteAsyncConnection _connection;

        private CategorySource _categorySource;
        private CategoryAdapter _categoryAdapter;

        private FloatingActionButton _addButton;
        private DialogEditCategory _addDialog;
        private DialogEditCategory _renameDialog;
        private int _renamePosition;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityEditCategories);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityEditCategories_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.editCategories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityEditCategories_buttonAdd);
            _addButton.Click += OnAddClick;

            _connection = await Database.Connect();
            _categorySource = new CategorySource(_connection);
            _categoryAdapter = new CategoryAdapter(_categorySource);
            _categoryAdapter.RenameClick += OnRenameClick;
            _categoryAdapter.DeleteClick += OnDeleteClick; 

            RecyclerView list = FindViewById<RecyclerView>(Resource.Id.activityEditCategories_list);
            list.SetAdapter(_categoryAdapter);
            list.HasFixedSize = true;
            list.SetItemViewCacheSize(20);
            list.DrawingCacheEnabled = true;
            list.DrawingCacheQuality = DrawingCacheQuality.High;

            LinearLayoutManager layout = new LinearLayoutManager(this);
            DividerItemDecoration decoration = new DividerItemDecoration(this, layout.Orientation);
            list.AddItemDecoration(decoration);
            list.SetLayoutManager(layout);

            await _categorySource.LoadTask;
            _categoryAdapter.NotifyDataSetChanged();
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            _addDialog = new DialogEditCategory(Resource.String.add, AddDialogPositive, AddDialogNegative);
            _addDialog.Show(transaction, "add_dialog");
        }

        private void AddDialogNegative(object sender, EventArgs e)
        {
            _addDialog.Dismiss();
        }

        private async void AddDialogPositive(object sender, EventArgs e)
        {
            if(_addDialog.Name.Trim() == "")
            {
                _addDialog.NameError = GetString(Resource.String.noCategoryName);
                return;
            }

            Category category = new Category(_addDialog.Name.Truncate(32));

            if(_categorySource.IsDuplicate(category))
            {
                _addDialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }

            _addDialog.Dismiss();
            await _connection.InsertAsync(category);
            await _categorySource.Update();
            _categoryAdapter.NotifyDataSetChanged();
        }

        private void OnRenameClick(object item, int position)
        {
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);

            string name = _categorySource.Categories[position].Name;
            _renameDialog = 
                new DialogEditCategory(Resource.String.rename, RenameDialogPositive, RenameDialogNegative, name);
            _renameDialog.Show(transaction, "rename_dialog");
            _renamePosition = position;
        }

        private void RenameDialogNegative(object sender, EventArgs e)
        {
            _renameDialog.Dismiss();
        }

        private async void RenameDialogPositive(object sender, EventArgs e)
        {
            if(_renameDialog.Name.Trim() == "")
            {
                _renameDialog.NameError = GetString(Resource.String.noCategoryName);
                return;
            }

            Category category = new Category(_renameDialog.Name.Truncate(32));

            if(_categorySource.IsDuplicate(category))
            {
                _renameDialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }

            _renameDialog.Dismiss();
            _categorySource.Rename(_renamePosition, _renameDialog.Name);
            _categoryAdapter.NotifyItemChanged(_renamePosition);
        }

        private void OnDeleteClick(object item, int position)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async (sender, args) =>
            {
                await _categorySource.Delete(position);
                _categoryAdapter.NotifyItemRemoved(position);
            });

            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });

            AlertDialog dialog = builder.Create();
            dialog.Show();
        }


        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}