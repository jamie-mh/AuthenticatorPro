using System;
using System.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Data.Source;
using AuthenticatorPro.Droid.Fragment;
using AuthenticatorPro.Droid.List;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Data;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;


namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class ManageCategoriesActivity : SensitiveSubActivity
    {
        private RelativeLayout _rootLayout;
        private LinearLayout _emptyStateLayout;
        private FloatingActionButton _addButton;
        private ManageCategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        private PreferenceWrapper _preferences;
        private CategorySource _categorySource;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityManageCategories);

            _preferences = new PreferenceWrapper(this);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.categories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _rootLayout = FindViewById<RelativeLayout>(Resource.Id.layoutRoot);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddClick;

            var connection = await Database.GetSharedConnection();
            _categorySource = new CategorySource(connection);
            _categoryListAdapter = new ManageCategoriesListAdapter(_categorySource);
            _categoryListAdapter.MenuClick += OnMenuClick;
            _categoryListAdapter.HasStableIds = true;
            _categoryListAdapter.DefaultId = _preferences.DefaultCategory;

            _categoryList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);

            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;

            var layout = new FixedGridLayoutManager(this, 1);
            _categoryList.SetLayoutManager(layout);

            var callback = new ReorderableListTouchHelperCallback(this, _categoryListAdapter, layout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_categoryList);

            var decoration = new DividerItemDecoration(this, layout.Orientation);
            _categoryList.AddItemDecoration(decoration);

            var layoutAnimation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _categoryList.LayoutAnimation = layoutAnimation;

            await Refresh();
        }

        private async Task Refresh()
        {
            await _categorySource.Update();
            
            RunOnUiThread(delegate
            {
                CheckEmptyState();

                _categoryListAdapter.NotifyDataSetChanged();
                _categoryList.ScheduleLayoutAnimation();
            });
        }

        private void CheckEmptyState()
        {
            if(_categorySource.GetView().Count == 0)
            {
                if(_categoryList.Visibility == ViewStates.Visible)
                    AnimUtil.FadeOutView(_categoryList, AnimUtil.LengthShort);

                if(_emptyStateLayout.Visibility == ViewStates.Invisible)
                    AnimUtil.FadeInView(_emptyStateLayout, AnimUtil.LengthLong);
            }
            else
            {
                if(_categoryList.Visibility == ViewStates.Invisible)
                    AnimUtil.FadeInView(_categoryList, AnimUtil.LengthLong);
                
                if(_emptyStateLayout.Visibility == ViewStates.Visible)
                    AnimUtil.FadeOutView(_emptyStateLayout, AnimUtil.LengthShort);
            }
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.New);
            
            var dialog = new EditCategoryBottomSheet {Arguments = bundle};
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
            var category = _categorySource.Get(position);

            if(category == null)
                return;

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutBoolean("isDefault", _preferences.DefaultCategory == category.Id);

            var fragment = new EditCategoryMenuBottomSheet {Arguments = bundle};
            fragment.ClickRename += OnRenameClick;
            fragment.ClickSetDefault += OnSetDefaultClick;
            fragment.ClickDelete += OnDeleteClick;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnRenameClick(object item, int position)
        {
            var category = _categorySource.Get(position);

            if(category == null)
                return;

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.Edit);
            bundle.PutInt("position", position);
            bundle.PutString("initialValue", category.Name);

            var fragment = new EditCategoryBottomSheet {Arguments = bundle};
            fragment.Submit += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs e)
        {
            var dialog = (EditCategoryBottomSheet) sender;

            if(e.Name == e.InitialName || e.Position == -1)
            {
                dialog.Dismiss();
                return;
            }

            var currentId = _categorySource.Get(e.Position).Id;
            var isDefault = _preferences.DefaultCategory == currentId;

            var category = new Category(e.Name);

            if(_categorySource.IsDuplicate(category))
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }

            try
            {
                await _categorySource.Rename(e.Position, e.Name);
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

            if(isDefault)
                SetDefaultCategory(category.Id);
                
            RunOnUiThread(delegate { _categoryListAdapter.NotifyItemChanged(e.Position); });
        }
        
        private void OnSetDefaultClick(object item, int position)
        {
            var category = _categorySource.Get(position);

            if(category == null)
                return;

            var oldDefault = _preferences.DefaultCategory;
            var isDefault = oldDefault == category.Id;

            SetDefaultCategory(isDefault ? null : category.Id);

            if(oldDefault != null)
            {
                var oldDefaultPos = _categorySource.GetPosition(oldDefault);
                
                if(oldDefaultPos > -1)
                    _categoryListAdapter.NotifyItemChanged(oldDefaultPos);
            }
            
            _categoryListAdapter.NotifyItemChanged(position);
        }

        private void OnDeleteClick(object item, int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                var category = _categorySource.Get(position);

                if(category == null)
                    return;

                if(_preferences.DefaultCategory == category.Id)
                    SetDefaultCategory(null);
                
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

        private void SetDefaultCategory(string id)
        {
            _preferences.DefaultCategory = _categoryListAdapter.DefaultId = id;
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