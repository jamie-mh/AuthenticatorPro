using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialog;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared;
using AuthenticatorPro.Shared.Util;
using Google.Android.Material.FloatingActionButton;
using SQLite;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;


namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class EditCategoriesActivity : LightDarkActivity
    {
        private LinearLayout _emptyStateLayout;
        private FloatingActionButton _addButton;
        private EditCategoryDialog _addDialog;
        private CategoryListAdapter _categoryListAdapter;
        private ProgressBar _progressBar;
        private RecyclerView _categoryList;
        private EditCategoryDialog _renameDialog;

        private CategorySource _categorySource;
        private SQLiteAsyncConnection _connection;
        private int _renamePosition;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityEditCategories);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityEditCategories_toolbar);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.activityEditCategories_progressBar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.editCategories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityEditCategories_buttonAdd);
            _addButton.Click += OnAddClick;

            _connection = await Database.Connect(this);
            _categorySource = new CategorySource(_connection);
            _categoryListAdapter = new CategoryListAdapter(_categorySource);
            _categoryListAdapter.RenameClick += OnRenameClick;
            _categoryListAdapter.DeleteClick += OnDeleteClick;

            _categoryList = FindViewById<RecyclerView>(Resource.Id.activityEditCategories_list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.activityEditCategories_emptyState);

            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetItemViewCacheSize(20);

            var callback = new ReorderableListTouchHelperCallback(_categoryListAdapter);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_categoryList);

            var layout = new LinearLayoutManager(this);
            var decoration = new DividerItemDecoration(this, layout.Orientation);
            _categoryList.AddItemDecoration(decoration);
            _categoryList.SetLayoutManager(layout);

            var layoutAnimation =
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _categoryList.LayoutAnimation = layoutAnimation;

            await Refresh();
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();
            await _connection.CloseAsync();
        }

        private async Task Refresh()
        {
            _progressBar.Visibility = ViewStates.Visible;

            await _categorySource.Update();
            CheckEmptyState();

            _categoryListAdapter.NotifyDataSetChanged();
            _categoryList.ScheduleLayoutAnimation();
            _progressBar.Visibility = ViewStates.Invisible;
        }

        private void CheckEmptyState()
        {
            if(_categorySource.Categories.Count == 0)
            {
                _categoryList.Visibility = ViewStates.Gone;
                AnimUtil.FadeInView(_emptyStateLayout, 500);
            }
            else
            {
                _emptyStateLayout.Visibility = ViewStates.Invisible;
                _categoryList.Visibility = ViewStates.Visible;
            }
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);
            _addDialog = new EditCategoryDialog(EditCategoryDialog.Mode.New);
            _addDialog.Submit += OnAddDialogSubmit;
            _addDialog.Show(transaction, "add_dialog");
        }

        private async void OnAddDialogSubmit(object sender, EventArgs e)
        {
            if(_addDialog.Name.Trim() == "")
            {
                _addDialog.Error = GetString(Resource.String.noCategoryName);
                return;
            }

            var category = new Category(_addDialog.Name);

            if(_categorySource.IsDuplicate(category))
            {
                _addDialog.Error = GetString(Resource.String.duplicateCategory);
                return;
            }

            _addDialog.Dismiss();
            await _connection.InsertAsync(category);
            await _categorySource.Update();
            _categoryListAdapter.NotifyDataSetChanged();
            CheckEmptyState();
        }

        private void OnRenameClick(object item, int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);

            var name = _categorySource.Categories[position].Name;
            _renameDialog = new EditCategoryDialog(EditCategoryDialog.Mode.Edit, name);
            _renameDialog.Submit += OnRenameDialogSubmit;
            _renameDialog.Show(transaction, "rename_dialog");
            _renamePosition = position;
        }

        private async void OnRenameDialogSubmit(object sender, EventArgs e)
        {
            if(_renameDialog.Name.Trim() == "")
            {
                _renameDialog.Error = GetString(Resource.String.noCategoryName);
                return;
            }

            var category = new Category(_renameDialog.Name);

            if(_categorySource.IsDuplicate(category))
            {
                _renameDialog.Error = GetString(Resource.String.duplicateCategory);
                return;
            }

            _renameDialog.Dismiss();
            await _categorySource.Rename(_renamePosition, _renameDialog.Name);
            _categoryListAdapter.NotifyItemChanged(_renamePosition);
        }

        private void OnDeleteClick(object item, int position)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async (sender, args) =>
            {
                await _categorySource.Delete(position);
                _categoryListAdapter.NotifyItemRemoved(position);
                CheckEmptyState();
            });

            builder.SetNegativeButton(Resource.String.cancel, (sender, args) => { });

            var dialog = builder.Create();
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