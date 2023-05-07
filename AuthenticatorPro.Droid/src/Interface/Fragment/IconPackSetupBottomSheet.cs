// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Service;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextView;
using ProtoBuf;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Object = Java.Lang.Object;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class IconPackSetupBottomSheet : BottomSheet
    {
        private readonly IIconPackView _iconPackView;
        private readonly IIconPackService _iconPackService;

        private IconPackListAdapter _iconPackListAdapter; 
        private ActivityResultLauncher _fileSelectResultLauncher;

        private RecyclerView _packList;
        private MaterialTextView _emptyText;
        private CircularProgressIndicator _progressIndicator;
        private MaterialButton _importButton;
        private MaterialButton _okButton;
        
        public IconPackSetupBottomSheet() : base(Resource.Layout.sheetIconPackSetup, Resource.String.prefIconPacksTitle)
        {
            _iconPackView = Dependencies.Resolve<IIconPackView>();
            _iconPackService = Dependencies.Resolve<IIconPackService>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var fileSelectCallback = new ActivityResultCallback();
            fileSelectCallback.Result += OnFileSelectResult;

            _fileSelectResultLauncher =
                RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), fileSelectCallback);
            
            _iconPackListAdapter = new IconPackListAdapter(_iconPackView);
            _iconPackListAdapter.HasStableIds = true;
            _iconPackListAdapter.DeleteClicked += OnDeleteClicked;
            _iconPackListAdapter.OpenUrlClicked += OnOpenUrlClicked;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            
            _packList = view.FindViewById<RecyclerView>(Resource.Id.listPack);
            _packList.SetAdapter(_iconPackListAdapter);
            _packList.HasFixedSize = true;
            _packList.SetItemAnimator(null);
            
            var layout = new FixedGridLayoutManager(RequireContext(), 1);
            _packList.SetLayoutManager(layout);
            _packList.AddItemDecoration(new GridSpacingItemDecoration(RequireContext(), layout, 12, false));
            
            _emptyText = view.FindViewById<MaterialTextView>(Resource.Id.textEmpty);
            _progressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.progressIndicator);

            _importButton = view.FindViewById<MaterialButton>(Resource.Id.buttonImport);
            _importButton.Click += OnImportButtonClick;
            
            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            _okButton.Click += delegate { Dismiss(); };

            return view;
        }
        
        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            
            await _iconPackView.LoadFromPersistenceAsync();
            _iconPackListAdapter.NotifyDataSetChanged();
            CheckEmptyState();
        }

        private void CheckEmptyState()
        {
            if (!_iconPackView.Any())
            {
                _packList.Visibility = ViewStates.Gone;
                _emptyText.Visibility = ViewStates.Visible;
            }
            else
            {
                _packList.Visibility = ViewStates.Visible;
                _emptyText.Visibility = ViewStates.Gone;
            }
        }

        private void OnDeleteClicked(object sender, IconPack pack)
        {
            var builder = new MaterialAlertDialogBuilder(RequireContext());
            builder.SetMessage(Resource.String.confirmIconPackDelete);
            builder.SetTitle(Resource.String.delete);
            builder.SetIcon(Resource.Drawable.baseline_delete_24);
            builder.SetCancelable(true);
            
            builder.SetPositiveButton(Resource.String.delete, async delegate
            {
                await _iconPackService.DeletePackAsync(pack);
                await _iconPackView.LoadFromPersistenceAsync();
                _iconPackListAdapter.NotifyDataSetChanged();
                CheckEmptyState();
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
                Toast.MakeText(RequireContext(), Resource.String.webBrowserMissing, ToastLength.Long).Show();
            }
        }

        private void OnImportButtonClick(object sender, EventArgs args)
        {
            var intent = new Intent(Intent.ActionGetContent);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("*/*");
            
            var baseApplication = ((SettingsActivity) Context).BaseApplication;
            baseApplication.PreventNextAutoLock = true;
            
            try
            {
                _fileSelectResultLauncher.Launch(intent);
            }
            catch (ActivityNotFoundException e)
            {
                Logger.Error(e);
                Toast.MakeText(Context, Resource.String.filePickerMissing, ToastLength.Long);
                baseApplication.PreventNextAutoLock = false;
            }
        }

        private async void OnFileSelectResult(object sender, Object obj)
        {
            var result = (ActivityResult) obj;
            var intent = result.Data;

            if ((Result) result.ResultCode != Result.Ok || intent.Data == null)
            {
                return;
            }
            
            SetLoading(true);

            try
            {
                await ImportIconPackAsync(intent.Data);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                Toast.MakeText(RequireContext(), Resource.String.importIconPackFailure, ToastLength.Short).Show();
                return;
            }
            finally
            {
                SetLoading(false);
            }

            await _iconPackView.LoadFromPersistenceAsync(); 
            _iconPackListAdapter.NotifyDataSetChanged();
            CheckEmptyState();
        }

        private async Task ImportIconPackAsync(Uri uri)
        {
            var data = await FileUtil.ReadFile(RequireContext(), uri);
            
            using var stream = new MemoryStream(data);
            var pack = await Task.Run(() => Serializer.Deserialize<IconPack>(stream));

            await _iconPackService.ImportPackAsync(pack);
           
            var message = String.Format(GetString(Resource.String.importIconPackSuccess), pack.Icons.Count);
            Toast.MakeText(RequireContext(), message, ToastLength.Long).Show();
        }

        private void SetLoading(bool loading)
        {
            SetCancelable(!loading);
            _importButton.Enabled = !loading;
            _okButton.Enabled = !loading;
            _progressIndicator.Visibility = loading ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}