// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using AuthenticatorPro.Droid.Shared.Util;
using Google.Android.Material.Button;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Tabs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class ChangeIconBottomSheet : BottomSheet
    {
        public event EventHandler<DefaultIconSelectedEventArgs> DefaultIconSelected;
        public event EventHandler<PackIconSelectedEventArgs> PackIconSelected;
        public event EventHandler UseCustomIconClick;

        private readonly IDefaultIconView _defaultIconView;
        private readonly IPackIconView _packIconView;
        private readonly IIconPackRepository _iconPackRepository;
        
        private string _secret;
        private List<IconPack> _iconPacks;

        private DefaultIconListAdapter _defaultIconListAdapter;
        private PackIconListAdapter _packIconListAdapter;

        private TabLayout _tabLayout;
        private EditText _searchText;
        private RecyclerView _iconList;
        private CircularProgressIndicator _progressIndicator;

        public ChangeIconBottomSheet() : base(Resource.Layout.sheetChangeIcon, Resource.String.changeIcon)
        {
            _defaultIconView = Dependencies.Resolve<IDefaultIconView>();
            _packIconView = Dependencies.Resolve<IPackIconView>();
            _iconPackRepository = Dependencies.Resolve<IIconPackRepository>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            _secret = Arguments.GetString("secret");
           
            var baseActivity = (BaseActivity) RequireActivity();
            _defaultIconView.UseDarkTheme = baseActivity.IsDark;
            _defaultIconView.Update();
            
            // Run sync so that view doesn't appear before it's finished
            _iconPacks = Task.Run(() => _iconPackRepository.GetAllAsync()).Result;
            
            _defaultIconListAdapter = new DefaultIconListAdapter(RequireContext(), _defaultIconView);
            _defaultIconListAdapter.ItemClicked += OnDefaultItemClicked;
            _defaultIconListAdapter.HasStableIds = true;
            
            _packIconListAdapter = new PackIconListAdapter(_packIconView);
            _packIconListAdapter.ItemClicked += OnPackItemClicked;
            _packIconListAdapter.HasStableIds = true;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            _progressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.progressIndicator);

            _iconList = view.FindViewById<RecyclerView>(Resource.Id.list);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);
            _iconList.SetItemAnimator(null);

            var layout = new AutoGridLayoutManager(Context, 140);
            _iconList.SetLayoutManager(layout);

            _searchText = view.FindViewById<EditText>(Resource.Id.editSearch);
            _searchText.TextChanged += OnSearchChanged;

            var customIconButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUseCustomIcon);
            customIconButton.Click += (s, e) =>
            {
                UseCustomIconClick?.Invoke(s, e);
                Dismiss();
            };

            _tabLayout = view.FindViewById<TabLayout>(Resource.Id.tabLayoutPack);
            _tabLayout.TabSelected += OnPackSelected;

            if (_iconPacks.Any())
            {
                var defaultTab = _tabLayout.NewTab();
                defaultTab.SetText(Resource.String.default_);
                _tabLayout.AddTab(defaultTab, true);

                foreach (var pack in _iconPacks)
                {
                    var tab = _tabLayout.NewTab();
                    tab.SetText(pack.Name);
                    _tabLayout.AddTab(tab);
                }
            }
            else
            {
                _tabLayout.Visibility = ViewStates.Gone;
                _iconList.Visibility = ViewStates.Visible;
                _progressIndicator.Visibility = ViewStates.Gone;
                _iconList.SetAdapter(_defaultIconListAdapter);
            }
            
            return view;
        }

        private async void OnPackSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            _iconList.Visibility = ViewStates.Invisible;
            _progressIndicator.Visibility = ViewStates.Visible;
            
            if (e.Tab.Position == 0)
            {
                _iconList.SetAdapter(_defaultIconListAdapter);
                _defaultIconView.Update();
                _defaultIconListAdapter.NotifyDataSetChanged();
            }
            else
            {
                var pack = _iconPacks[e.Tab.Position - 1];
                await _packIconView.LoadFromPersistenceAsync(pack);
                _iconList.SetAdapter(_packIconListAdapter);
                _packIconListAdapter.NotifyDataSetChanged();
            }
            
            _progressIndicator.Visibility = ViewStates.Gone;
            AnimUtil.FadeInView(_iconList, AnimUtil.LengthShort);
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (_tabLayout.SelectedTabPosition == 0)
            {
                _defaultIconView.Search = e.Text.ToString();
                _defaultIconListAdapter.NotifyDataSetChanged();
            }
            else
            {
                _packIconView.Search = e.Text.ToString();
                _packIconListAdapter.NotifyDataSetChanged();
            }
        }

        private void OnDefaultItemClicked(object sender, int iconPosition)
        {
            var eventArgs = new DefaultIconSelectedEventArgs(_secret, _defaultIconView[iconPosition].Key);
            DefaultIconSelected?.Invoke(this, eventArgs);
        }
        
        private void OnPackItemClicked(object sender, Bitmap bitmap)
        {
            var eventArgs = new PackIconSelectedEventArgs(_secret, bitmap);
            PackIconSelected?.Invoke(this, eventArgs);
        }

        public class DefaultIconSelectedEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly string Icon;

            public DefaultIconSelectedEventArgs(string secret, string icon)
            {
                Secret = secret;
                Icon = icon;
            }
        }
        
        public class PackIconSelectedEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly Bitmap Icon;

            public PackIconSelectedEventArgs(string secret, Bitmap icon)
            {
                Secret = secret;
                Icon = icon;
            }
        }
    }
}