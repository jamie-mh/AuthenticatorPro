// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.Core.Graphics;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence.Exception;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Interface;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.Fragment;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared.Util;
using Google.Android.Material.Dialog;
using Google.Android.Material.Internal;
using Google.Android.Material.Snackbar;
using Serilog;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    public class CategoriesActivity : SensitiveSubActivity
    {
        private readonly ILogger _log = Log.ForContext<CategoriesActivity>();
        private readonly ICategoryView _categoryView;
        private readonly ICategoryService _categoryService;

        private LinearLayout _emptyStateLayout;
        private CategoryListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;

        public CategoriesActivity() : base(Resource.Layout.activityCategories)
        {
            _categoryView = Dependencies.Resolve<ICategoryView>();
            _categoryService = Dependencies.Resolve<ICategoryService>();
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SupportActionBar.SetTitle(Resource.String.categories);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            AddButton.Click += OnAddClick;

            _categoryListAdapter = new CategoryListAdapter(_categoryView);
            _categoryListAdapter.MenuClicked += OnMenuClicked;
            _categoryListAdapter.MovementFinished += OnMovementFinished;
            _categoryListAdapter.HasStableIds = true;
            _categoryListAdapter.DefaultId = Preferences.DefaultCategory;

            _categoryList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);

            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;

            var layout = new FixedGridLayoutManager(this, 1);
            _categoryList.SetLayoutManager(layout);

            var callback = new ReorderableListTouchHelperCallback(this, _categoryListAdapter, layout);
            var touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(_categoryList);

            _categoryList.AddItemDecoration(new GridSpacingItemDecoration(this, layout, 12, true));

            var layoutAnimation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _categoryList.LayoutAnimation = layoutAnimation;

            await _categoryView.LoadFromPersistenceAsync();

            RunOnUiThread(delegate
            {
                CheckEmptyState();

                _categoryListAdapter.NotifyDataSetChanged();
                _categoryList.ScheduleLayoutAnimation();
            });
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

            await _categoryService.UpdateManyCategoriesAsync(_categoryView);
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
            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.New);

            var fragment = new EditCategoryBottomSheet { Arguments = bundle };
            fragment.Submitted += OnAddDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAddDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs args)
        {
            var dialog = (EditCategoryBottomSheet) sender;
            var category = new Category(args.Name);

            try
            {
                await _categoryService.AddCategoryAsync(category);
            }
            catch (EntityDuplicateException)
            {
                dialog.NameError = GetString(Resource.String.duplicateCategory);
                return;
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to add category");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                RunOnUiThread(dialog.Dismiss);
                return;
            }

            await _categoryView.LoadFromPersistenceAsync();

            RunOnUiThread(delegate
            {
                _categoryListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
                dialog.Dismiss();
            });
        }

        private void OnMenuClicked(object sender, string id)
        {
            var bundle = new Bundle();
            bundle.PutString("id", id);
            bundle.PutBoolean("isDefault", Preferences.DefaultCategory == id);

            var fragment = new EditCategoryMenuBottomSheet { Arguments = bundle };
            fragment.RenameClicked += OnRenameClicked;
            fragment.AssignEntriesClicked += OnAssignEntriesClick;
            fragment.SetDefaultClicked += OnSetDefaultClick;
            fragment.DeleteClicked += OnDeleteClick;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private void OnRenameClicked(object item, string id)
        {
            var category = _categoryView.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return;
            }

            var bundle = new Bundle();
            bundle.PutInt("mode", (int) EditCategoryBottomSheet.Mode.Edit);
            bundle.PutString("id", id);
            bundle.PutString("initialValue", category.Name);

            var fragment = new EditCategoryBottomSheet { Arguments = bundle };
            fragment.Submitted += OnRenameDialogSubmit;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAssignEntriesClick(object item, string id)
        {
            var category = _categoryView.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return;
            }

            var bindings = await _categoryService.GetBindingsForCategoryAsync(category);
            var authenticatorSecrets = bindings.Select(ac => ac.AuthenticatorSecret).ToArray();

            var bundle = new Bundle();
            bundle.PutString("id", id);
            bundle.PutStringArray("assignedAuthenticatorSecrets", authenticatorSecrets);

            var fragment = new AssignCategoryEntriesBottomSheet { Arguments = bundle };
            fragment.AuthenticatorClicked += OnAssignEntriesAuthenticatorClicked;
            fragment.Show(SupportFragmentManager, fragment.Tag);
        }

        private async void OnAssignEntriesAuthenticatorClicked(object sender,
            AssignCategoryEntriesBottomSheet.AuthenticatorClickedEventArgs args)
        {
            var category = _categoryView.FirstOrDefault(c => c.Id == args.CategoryId);

            if (category == null)
            {
                return;
            }

            try
            {
                if (args.IsChecked)
                {
                    await _categoryService.AddBindingAsync(args.Authenticator, category);
                }
                else
                {
                    await _categoryService.RemoveBindingAsync(args.Authenticator, category);
                }
            }
            catch (Exception e)
            {
                _log.Error(e, "Failed to assign entry");
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
            }
        }

        private async void OnRenameDialogSubmit(object sender, EditCategoryBottomSheet.EditCategoryEventArgs args)
        {
            var dialog = (EditCategoryBottomSheet) sender;
            var initial = _categoryView.FirstOrDefault(c => c.Id == args.Id);

            if (initial == null || args.Name == args.InitialName)
            {
                dialog.Dismiss();
                return;
            }

            var isDefault = Preferences.DefaultCategory == initial.Id;
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
                _log.Error(e, "Failed to transfer category bindings");
                RunOnUiThread(dialog.Dismiss);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                return;
            }

            if (isDefault)
            {
                SetDefaultCategory(next.Id);
            }

            var position = _categoryView.IndexOf(args.Id);
            await _categoryView.LoadFromPersistenceAsync();

            RunOnUiThread(delegate
            {
                _categoryListAdapter.NotifyItemChanged(position);
                dialog.Dismiss();
            });
        }

        private void OnSetDefaultClick(object item, string id)
        {
            var category = _categoryView.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return;
            }

            var oldDefault = Preferences.DefaultCategory;
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

            var position = _categoryView.IndexOf(id);
            _categoryListAdapter.NotifyItemChanged(position);
        }

        private void OnDeleteClick(object item, string id)
        {
            var category = _categoryView.FirstOrDefault(c => c.Id == id);

            if (category == null)
            {
                return;
            }

            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmCategoryDelete);
            builder.SetTitle(Resource.String.delete);
            builder.SetIcon(Resource.Drawable.baseline_delete_24);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                if (Preferences.DefaultCategory == category.Id)
                {
                    SetDefaultCategory(null);
                }

                try
                {
                    await _categoryService.DeleteWithCategoryBindingsASync(category);
                }
                catch (Exception e)
                {
                    _log.Error(e, "Failed to delete category with bindings");
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                    return;
                }

                var position = _categoryView.IndexOf(id);
                await _categoryView.LoadFromPersistenceAsync();

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
            Preferences.DefaultCategory = _categoryListAdapter.DefaultId = id;
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

        protected override void OnApplySystemBarInsets(Insets insets)
        {
            base.OnApplySystemBarInsets(insets);

            var bottomPadding = (int) ViewUtils.DpToPx(this, ListFabPaddingBottom) + insets.Bottom;
            _categoryList.SetPadding(0, 0, 0, bottomPadding);

            var layoutParams = (ViewGroup.MarginLayoutParams) AddButton.LayoutParameters;
            layoutParams.SetMargins(layoutParams.LeftMargin, layoutParams.TopMargin, layoutParams.RightMargin,
                layoutParams.BottomMargin + insets.Bottom);
        }
    }
}