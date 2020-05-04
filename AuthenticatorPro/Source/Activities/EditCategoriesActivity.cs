using System;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.AuthenticatorList;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialogs;
using AuthenticatorPro.CategoryList;
using Google.Android.Material.FloatingActionButton;
using SQLite;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity]
    internal class EditCategoriesActivity : LightDarkActivity
    {
        private FloatingActionButton _addButton;
        private EditCategoryDialog _addDialog;
        private CategoryAdapter _categoryAdapter;

        private RecyclerView _categoryList;

        private CategorySource _categorySource;
        private SQLiteAsyncConnection _connection;
        private LinearLayout _emptyState;
        private EditCategoryDialog _renameDialog;
        private int _renamePosition;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityEditCategories);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityEditCategories_toolbar);
            var progressBar = FindViewById<ProgressBar>(Resource.Id.activityEditCategories_progressBar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.editCategories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.activityEditCategories_buttonAdd);
            _addButton.Click += OnAddClick;

            _connection = await Database.Connect(this);
            _categorySource = new CategorySource(_connection);
            _categoryAdapter = new CategoryAdapter(_categorySource);
            _categoryAdapter.RenameClick += OnRenameClick;
            _categoryAdapter.DeleteClick += OnDeleteClick;

            _categoryList = FindViewById<RecyclerView>(Resource.Id.activityEditCategories_list);
            _emptyState = FindViewById<LinearLayout>(Resource.Id.activityEditCategories_emptyState);

            _categoryList.SetAdapter(_categoryAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetItemViewCacheSize(20);

            var callback = new AuthListTouchHelperCallback(_categoryAdapter);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_categoryList);

            var layout = new LinearLayoutManager(this);
            var decoration = new DividerItemDecoration(this, layout.Orientation);
            _categoryList.AddItemDecoration(decoration);
            _categoryList.SetLayoutManager(layout);

            var layoutAnimation =
                AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _categoryList.LayoutAnimation = layoutAnimation;

            await _categorySource.UpdateTask;
            CheckEmptyState();
            _categoryAdapter.NotifyDataSetChanged();
            _categoryList.ScheduleLayoutAnimation();

            var alphaAnimation = new AlphaAnimation(1.0f, 0.0f) {
                Duration = 200
            };
            alphaAnimation.AnimationEnd += (sender, e) => { progressBar.Visibility = ViewStates.Gone; };
            progressBar.StartAnimation(alphaAnimation);
        }

        private void CheckEmptyState()
        {
            if(_categorySource.Count() == 0)
            {
                _emptyState.Visibility = ViewStates.Visible;
                _categoryList.Visibility = ViewStates.Gone;

                var animation = new AlphaAnimation(0.0f, 1.0f) {
                    Duration = 500
                };
                _emptyState.Animation = animation;
            }
            else
            {
                _emptyState.Visibility = ViewStates.Gone;
                _categoryList.Visibility = ViewStates.Visible;
            }
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _addDialog = new EditCategoryDialog(Resource.String.add, AddDialogPositive, AddDialogNegative);
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
                _addDialog.Error = GetString(Resource.String.noCategoryName);
                return;
            }

            var category = new Category(_addDialog.Name.Truncate(32));

            if(_categorySource.IsDuplicate(category))
            {
                _addDialog.Error = GetString(Resource.String.duplicateCategory);
                return;
            }

            _addDialog.Dismiss();
            await _connection.InsertAsync(category);
            await _categorySource.Update();
            _categoryAdapter.NotifyDataSetChanged();
            CheckEmptyState();
        }

        private void OnRenameClick(object item, int position)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("rename_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);

            var name = _categorySource.Categories[position].Name;
            _renameDialog =
                new EditCategoryDialog(Resource.String.rename, RenameDialogPositive, RenameDialogNegative, name);
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
                _renameDialog.Error = GetString(Resource.String.noCategoryName);
                return;
            }

            var category = new Category(_renameDialog.Name.Truncate(32));

            if(_categorySource.IsDuplicate(category))
            {
                _renameDialog.Error = GetString(Resource.String.duplicateCategory);
                return;
            }

            _renameDialog.Dismiss();
            await _categorySource.Rename(_renamePosition, _renameDialog.Name);
            _categoryAdapter.NotifyItemChanged(_renamePosition);
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
                _categoryAdapter.NotifyItemRemoved(position);
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