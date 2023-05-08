// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared.Util;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Interface;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Dialog;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Activity
{
    [Activity]
    internal class IconPacksActivity : SensitiveSubActivity
    {
        private const int RequestAdd = 0;
        
        private CoordinatorLayout _rootLayout;
        private LinearProgressIndicator _progressIndicator;
        private LinearLayout _emptyStateLayout;
        private FloatingActionButton _addButton;
        private IconPackListAdapter _iconPackListAdapter;
        private RecyclerView _packList;

        private readonly IIconPackView _iconPackView;
        private readonly IIconPackService _iconPackService;

        public IconPacksActivity() : base(Resource.Layout.activityIconPacks)
        {
            _iconPackView = Dependencies.Resolve<IIconPackView>();
            _iconPackService = Dependencies.Resolve<IIconPackService>();
        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.iconPacks);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.baseline_arrow_back_24);

            _rootLayout = FindViewById<CoordinatorLayout>(Resource.Id.layoutRoot);
            _progressIndicator = FindViewById<LinearProgressIndicator>(Resource.Id.progressIndicator);

            _addButton = FindViewById<FloatingActionButton>(Resource.Id.buttonAdd);
            _addButton.Click += OnAddClick;

            _iconPackListAdapter = new IconPackListAdapter(_iconPackView);
            _iconPackListAdapter.HasStableIds = true;
            _iconPackListAdapter.DeleteClicked += OnDeleteClicked;
            _iconPackListAdapter.OpenUrlClicked += OnOpenUrlClicked;

            _packList = FindViewById<RecyclerView>(Resource.Id.list);
            _emptyStateLayout = FindViewById<LinearLayout>(Resource.Id.layoutEmptyState);

            _packList.SetAdapter(_iconPackListAdapter);
            _packList.HasFixedSize = true;

            var layout = new FixedGridLayoutManager(this, 1);
            _packList.SetLayoutManager(layout);
            _packList.AddItemDecoration(new GridSpacingItemDecoration(this, layout, 12, true));
            
            var layoutAnimation = AnimationUtils.LoadLayoutAnimation(this, Resource.Animation.layout_animation_fade_in);
            _packList.LayoutAnimation = layoutAnimation;

            await _iconPackView.LoadFromPersistenceAsync();

            RunOnUiThread(delegate
            {
                CheckEmptyState();

                _iconPackListAdapter.NotifyDataSetChanged();
                _packList.ScheduleLayoutAnimation();
            });
        }
        
        protected override async void OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            if (requestCode != RequestAdd || resultCode != Result.Ok)
            {
                return;
            }
            
            SetLoading(true);
            MemoryStream stream = null;
            IconPack pack;

            try
            {
                var data = await FileUtil.ReadFile(this, intent.Data);
                stream = new MemoryStream(data);
                pack = await Task.Run(() => Serializer.Deserialize<IconPack>(stream));
            }
            catch (ProtoException e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.invalidIconPackError, Snackbar.LengthShort);
                SetLoading(false);
                return;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.filePickError, Snackbar.LengthShort);
                SetLoading(false);
                return;
            }
            finally
            {
                stream?.Close();
            }

            try
            {
                await _iconPackService.ImportPackAsync(pack);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                SetLoading(false);
                return;
            }
            
            var message = String.Format(GetString(Resource.String.importIconPackSuccess), pack.Icons.Count);
            ShowSnackbar(message, Snackbar.LengthLong);

            await _iconPackView.LoadFromPersistenceAsync();
            SetLoading(false);
            
            RunOnUiThread(delegate
            {
                CheckEmptyState();
                _iconPackListAdapter.NotifyDataSetChanged();
            });
        }

        private void CheckEmptyState()
        {
            if (!_iconPackView.Any())
            {
                if (_packList.Visibility == ViewStates.Visible)
                {
                    AnimUtil.FadeOutView(_packList, AnimUtil.LengthShort);
                }

                if (_emptyStateLayout.Visibility == ViewStates.Invisible)
                {
                    AnimUtil.FadeInView(_emptyStateLayout, AnimUtil.LengthLong);
                }
            }
            else
            {
                if (_packList.Visibility == ViewStates.Invisible)
                {
                    AnimUtil.FadeInView(_packList, AnimUtil.LengthLong);
                }

                if (_emptyStateLayout.Visibility == ViewStates.Visible)
                {
                    AnimUtil.FadeOutView(_emptyStateLayout, AnimUtil.LengthShort);
                }
            }
        }

        private void OnAddClick(object sender, EventArgs args)
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");

            BaseApplication.PreventNextAutoLock = true;
            
            try
            {
                StartActivityForResult(intent, RequestAdd);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.filePickerMissing, Snackbar.LengthLong);
                BaseApplication.PreventNextAutoLock = false;
            }
        }
        
        private void OnDeleteClicked(object sender, IconPack pack)
        {
            var builder = new MaterialAlertDialogBuilder(this);
            builder.SetMessage(Resource.String.confirmIconPackDelete);
            builder.SetTitle(Resource.String.delete);
            builder.SetIcon(Resource.Drawable.baseline_delete_24);
            builder.SetCancelable(true);
            
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                try
                {
                    await _iconPackService.DeletePackAsync(pack);
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                    ShowSnackbar(Resource.String.genericError, Snackbar.LengthShort);
                }
                
                var position = _iconPackView.IndexOf(pack.Name);
                await _iconPackView.LoadFromPersistenceAsync();
                
                RunOnUiThread(delegate
                {
                    _iconPackListAdapter.NotifyItemRemoved(position);
                    CheckEmptyState();
                });
            });

            builder.SetNegativeButton(Resource.String.cancel, delegate { });

            var dialog = builder.Create();
            dialog.Show();
        }

        private void OnOpenUrlClicked(object sender, IconPack pack)
        {
            var intent = new Intent(Intent.ActionView, Uri.Parse(pack.Url));

            try
            {
                StartActivity(intent);
            }
            catch (ActivityNotFoundException)
            {
                ShowSnackbar(Resource.String.webBrowserMissing, Snackbar.LengthLong);
            }
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
        
        private void SetLoading(bool loading)
        {
            RunOnUiThread(delegate
            {
                _progressIndicator.Visibility = loading ? ViewStates.Visible : ViewStates.Invisible;
            });
        }

        private void ShowSnackbar(int textRes, int length)
        {
            var snackbar = Snackbar.Make(_rootLayout, textRes, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }
        
        private void ShowSnackbar(string message, int length)
        {
            var snackbar = Snackbar.Make(_rootLayout, message, length);
            snackbar.SetAnchorView(_addButton);
            snackbar.Show();
        }
    }
}