// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Interface;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.Fragment;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using AuthenticatorPro.Shared.Service;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;
using System;
using System.Linq;
using System.Threading.Tasks;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class EditCategoriesActivity : SensitiveSubActivity
    {
        private RelativeLayout _rootLayout;
        private LinearLayout _emptyStateLayout;
        private FloatingActionButton _addButton;
        private ManageCategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        private PreferenceWrapper _preferences;

        private readonly ICategoryView _categoryView;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICategoryService _categoryService;

        public EditCategoriesActivity() : base(Resource.Layout.activityEditCategories)
        {
            _categoryView = Dependencies.Resolve<ICategoryView>();
            _categoryRepository = Dependencies.Resolve<ICategoryRepository>();
            _categoryService = Dependencies.Resolve<ICategoryService>();
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _preferences = new PreferenceWrapper(this);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.categories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            _rootLayout = FindViewById<RelativeLayout>(Resource.Id.layoutRoot);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddClick;

            _categoryListAdapter = new ManageCategoriesListAdapter(_categoryView);
            _categoryListAdapter.MenuClicked += OnMenuClicked;
            _categoryListAdapter.MovementFinished += OnMovementFinished;
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

        private async void OnMovementFinished(object sender, bool orderChanged)
        {
            if (!orderChanged)
            {
                return;
            }

            for (var i = 0; i < _categoryView.Count; ++i)
            {
                _categoryView[i].Ranking = i;
            }

            await _categoryService.UpdateManyAsync(_categoryView);
        }

        private async Task Refresh()
        {
            await _categoryView.LoadFromPersistence();

            RunOnUiThread(delegate
            {
                CheckEmptyState();

                _categoryListAdapter.NotifyDataSetChanged();
                _categoryList.ScheduleLayoutAnimation();
            });
        }

        private void CheckEmptyState()
        {
            if (!_categoryView.Any())
            {
                if (_categoryList.Visibility == ViewStates.Visible)
                {
                    AnimUtil.FadeOutView(_categoryList, AnimUtil.LengthShort);
                }

                if (_emptyStateLayout.Visibility == ViewStates.Invisible)
                {
                    AnimUtil.FadeInView(_emptyStateLayout, AnimUtil.LengthLong);
                }
            }
            else
            {
                if (_categoryList.Visibility == ViewStates.Invisible)
                {
                    AnimUtil.FadeInView(_categoryList, AnimUtil.LengthLong);
                }

                if (_emptyStateLayout.Visibility == ViewStates.Visible)
                {
                    AnimUtil.FadeOutView(_emptyStateLayout, AnimUtil.LengthShort);
                }
            }
        }

        private void OnAddClick(object sender, EventArgs e)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("add_dialog");

            if (old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.New);

            var dialog = new EditCategoryBottomSheet { Arguments = bundle };
            dialog.Submitted += OnAddDialogSubmit;
            dialog.Show(transaction, "add_dialog");
        }

        private async void OnAddDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs args)
        {
            var dialog = (EditCategoryBottomSheet) sender;
            var category = new Category(args.Name);

            try
            {
                await _categoryRepository.CreateAsync(category);
            }
            catch (EntityDuplicateException)
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                RunOnUiThread(dialog.Dismiss);
                return;
            }

            await _categoryView.LoadFromPersistence();

            RunOnUiThread(delegate
            {
                _categoryListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
                dialog.Dismiss();
            });
        }

        private void OnMenuClicked(object sender, int position)
        {
            var category = _categoryView.ElementAt(position);

            var bundle = new Bundle();
            bundle.PutInt("position", position);
            bundle.PutBoolean("isDefault", _preferences.DefaultCategory == category.Id);

            var fragment = new EditCategoryMenuBottomSheet { Arguments = bundle };
            fragment.RenameClicked += OnRenameClickedClick;
            fragment.SetDefaultClicked += OnSetDefaultClickedClick;
            fragment.DeleteClicked += OnDeleteClickedClick;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnRenameClickedClick(object item, int position)
        {
            var category = _categoryView.ElementAt(position);

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.Edit);
            bundle.PutInt("position", position);
            bundle.PutString("initialValue", category.Name);

            var fragment = new EditCategoryBottomSheet { Arguments = bundle };
            fragment.Submitted += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnRenameDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs args)
        {
            var dialog = (EditCategoryBottomSheet) sender;

            if (args.Name == args.InitialName || args.Position == -1)
            {
                dialog.Dismiss();
                return;
            }

            var initial = _categoryView.ElementAt(args.Position);
            var isDefault = _preferences.DefaultCategory == initial.Id;
            var next = new Category(args.Name) { Ranking = initial.Ranking };

            try
            {
                await _categoryService.TransferAsync(initial, next);
            }
            catch (EntityDuplicateException)
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                RunOnUiThread(dialog.Dismiss);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if (isDefault)
            {
                SetDefaultCategory(next.Id);
            }

            await _categoryView.LoadFromPersistence();

            RunOnUiThread(delegate
            {
                _categoryListAdapter.NotifyItemChanged(args.Position);
                dialog.Dismiss();
            });
        }

        private void OnSetDefaultClickedClick(object item, int position)
        {
            var category = _categoryView.ElementAt(position);
            var oldDefault = _preferences.DefaultCategory;
            var isDefault = oldDefault == category.Id;

            SetDefaultCategory(isDefault ? null : category.Id);

            if (oldDefault != null)
            {
                var oldDefaultPos = _categoryView.IndexOf(oldDefault);

                if (oldDefaultPos > -1)
                {
                    _categoryListAdapter.NotifyItemChanged(oldDefaultPos);
                }
            }

            _categoryListAdapter.NotifyItemChanged(position);
        }

        private void OnDeleteClickedClick(object item, int position)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.warning);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                var category = _categoryView.ElementAt(position);

                if (_preferences.DefaultCategory == category.Id)
                {
                    SetDefaultCategory(null);
                }

                try
                {
                    await _categoryService.DeleteWithCategoryBindingsASync(category);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                await _categoryView.LoadFromPersistence();

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
            if (item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ShowSnackbar(int textRes, int length)
        {
            var snackbar = Snackbar.Make(_rootLayout, textRes, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }
    }
}