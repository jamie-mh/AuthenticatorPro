using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Data.Source;
using AuthenticatorPro.Fragment;
using AuthenticatorPro.List;
using AuthenticatorPro.Shared.Util;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using SQLite;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;


namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class ManageCategoriesActivity : DayNightActivity
    {
        private RelativeLayout _rootLayout;
        private LinearLayout _emptyStateLayout;
        private FloatingActionButton _addButton;
        private ManageCategoriesListAdapter _categoryListAdapter;
        private ProgressBar _progressBar;
        private RecyclerView _categoryList;

        private CategorySource _categorySource;
        private SQLiteAsyncConnection _connection;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityManageCategories);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.categories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _rootLayout = FindViewById<RelativeLayout>(Resource.Id.layoutRoot);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddClick;

            _connection = await Database.Connect(this);
            _categorySource = new CategorySource(_connection);
            _categoryListAdapter = new ManageCategoriesListAdapter(_categorySource);
            _categoryListAdapter.MenuClick += OnMenuClick;
            _categoryListAdapter.HasStableIds = true;

            _categoryList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);

            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;

            var layout = new FixedGridLayoutManager(this, 1);
            _categoryList.SetLayoutManager(layout);

            var callback = new ReorderableListTouchHelperCallback(_categoryListAdapter, layout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_categoryList);

            var decoration = new DividerItemDecoration(this, layout.Orientation);
            _categoryList.AddItemDecoration(decoration);

            var layoutAnimation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _categoryList.LayoutAnimation = layoutAnimation;

            await Refresh();
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();

            if(_connection != null)
                await _connection.CloseAsync();
        }

        private async Task Refresh()
        {
            RunOnUiThread(delegate { _progressBar.Visibility = ViewStates.Visible; });

            await _categorySource.Update();
            
            RunOnUiThread(delegate
            {
                CheckEmptyState();

                _categoryListAdapter.NotifyDataSetChanged();
                _categoryList.ScheduleLayoutAnimation();
                _progressBar.Visibility = ViewStates.Invisible;
            });
        }

        private void CheckEmptyState()
        {
            if(_categorySource.GetView().Count == 0)
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
            var dialog = new EditCategoryBottomSheet(EditCategoryBottomSheet.Mode.New, null);
            dialog.Submit += OnAddDialogSubmit;
            dialog.Show(transaction, "add_dialog");
        }

        private async void OnAddDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs e)
        {
            var dialog = (EditCategoryBottomSheet) sender;
            var category = new Category(e.Name);

            if(_categorySource.IsDuplicate(category))
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }

            try
            {
                await _categorySource.Add(category);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            finally
            {
                RunOnUiThread(dialog.Dismiss); 
            }
            
            RunOnUiThread(delegate
            {
                _categoryListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
            });
        }

        private void OnMenuClick(object sender, int position)
        {
            var fragment = new EditCategoryMenuBottomSheet(position);
            fragment.ClickRename += OnRenameClick;
            fragment.ClickDelete += OnDeleteClick;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnRenameClick(object item, int position)
        {
            var category = _categorySource.Get(position);

            if(category == null)
                return;

            var fragment = new EditCategoryBottomSheet(EditCategoryBottomSheet.Mode.Edit, position, category.Name);
            fragment.Submit += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs e)
        {
            var dialog = (EditCategoryBottomSheet) sender;

            if(e.Name == e.InitialName || e.ItemPosition == null)
            {
                dialog.Dismiss();
                return;
            }

            var category = new Category(e.Name);

            if(_categorySource.IsDuplicate(category))
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }

            try
            {
                await _categorySource.Rename(e.ItemPosition.Value, e.Name);
            }
            catch
            {
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }
            finally
            {
                RunOnUiThread(dialog.Dismiss); 
            }
            
            RunOnUiThread(delegate { _categoryListAdapter.NotifyItemChanged(e.ItemPosition.Value); });
        }

        private void OnDeleteClick(object item, int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                try
                {
                    await _categorySource.Delete(position);
                }
                catch
                {
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }
                
                RunOnUiThread(delegate
                {
                    _categoryListAdapter.NotifyItemRemoved(position);
                    CheckEmptyState();
                });
            });

            builder.SetNegativeButton(Resource.String.cancel, delegate { });

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
        
        private void ShowSnackbar(int textRes, int length)
        {
            var snackbar = Snackbar.Make(_rootLayout, textRes, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }
    }
}