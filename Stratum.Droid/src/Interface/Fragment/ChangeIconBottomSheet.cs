// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Linq;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Stratum.Droid.Shared.Util;
using Google.Android.Material.Button;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Tabs;
using Stratum.Droid.Activity;
using Stratum.Droid.Interface.Adapter;
using Stratum.Droid.Interface.LayoutManager;
using Stratum.Droid.Persistence.View;

namespace Stratum.Droid.Interface.Fragment
{
    public class ChangeIconBottomSheet : BottomSheet
    {
        private readonly IDefaultIconView _defaultIconView;
        private readonly IIconPackView _iconPackView;
        private readonly IIconPackEntryView _iconPackEntryView;

        private string _secret;

        private DefaultIconListAdapter _defaultIconListAdapter;
        private IconPackEntryListAdapter _iconPackEntryListAdapter;

        private TabLayout _tabLayout;
        private EditText _searchText;
        private RecyclerView _iconList;
        private CircularProgressIndicator _progressIndicator;

        public ChangeIconBottomSheet() : base(Resource.Layout.sheetChangeIcon, Resource.String.changeIcon)
        {
            _defaultIconView = Dependencies.Resolve<IDefaultIconView>();
            _iconPackView = Dependencies.Resolve<IIconPackView>();
            _iconPackEntryView = Dependencies.Resolve<IIconPackEntryView>();
        }

        public event EventHandler<DefaultIconSelectedEventArgs> DefaultIconSelected;
        public event EventHandler<IconPackEntrySelectedEventArgs> IconPackEntrySelected;
        public event EventHandler UseCustomIconClick;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _secret = Arguments.GetString("secret");

            var baseActivity = (BaseActivity) RequireActivity();
            _defaultIconView.UseDarkTheme = baseActivity.IsDark;
            _defaultIconView.Update();

            _defaultIconListAdapter = new DefaultIconListAdapter(RequireContext(), _defaultIconView);
            _defaultIconListAdapter.ItemClicked += OnDefaultItemClicked;
            _defaultIconListAdapter.HasStableIds = true;

            _iconPackEntryListAdapter = new IconPackEntryListAdapter(_iconPackEntryView);
            _iconPackEntryListAdapter.ItemClicked += OnIconPackEntryClicked;
            _iconPackEntryListAdapter.HasStableIds = true;
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

            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            await _iconPackView.LoadFromPersistenceAsync();

            _tabLayout = view.FindViewById<TabLayout>(Resource.Id.tabLayoutPack);
            _tabLayout.TabSelected += OnPackSelected;

            if (_iconPackView.Any())
            {
                var defaultTab = _tabLayout.NewTab();
                defaultTab.SetText(Resource.String.defaultIcons);
                _tabLayout.AddTab(defaultTab, true);

                foreach (var pack in _iconPackView)
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
        }

        private async void OnPackSelected(object sender, TabLayout.TabSelectedEventArgs e)
        {
            _iconList.Visibility = ViewStates.Invisible;
            _progressIndicator.Visibility = ViewStates.Visible;

            if (e.Tab.Position == 0)
            {
                _iconList.SetAdapter(_defaultIconListAdapter);
                _defaultIconView.Search = _searchText.Text;
                _defaultIconListAdapter.NotifyDataSetChanged();
            }
            else
            {
                var pack = _iconPackView[e.Tab.Position - 1];
                await _iconPackEntryView.LoadFromPersistenceAsync(pack);
                _iconPackEntryView.Search = _searchText.Text;
                _iconList.SetAdapter(_iconPackEntryListAdapter);
                _iconPackEntryListAdapter.NotifyDataSetChanged();
            }

            _progressIndicator.Visibility = ViewStates.Gone;
            AnimUtil.FadeInView(_iconList, AnimUtil.LengthShort);
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            if (!_iconPackView.Any() || _tabLayout.SelectedTabPosition == 0)
            {
                _defaultIconView.Search = e.Text.ToString();
                _defaultIconListAdapter.NotifyDataSetChanged();
            }
            else
            {
                _iconPackEntryView.Search = e.Text.ToString();
                _iconPackEntryListAdapter.NotifyDataSetChanged();
            }
        }

        private void OnDefaultItemClicked(object sender, int iconPosition)
        {
            var eventArgs = new DefaultIconSelectedEventArgs(_secret, _defaultIconView[iconPosition].Key);
            DefaultIconSelected?.Invoke(this, eventArgs);
        }

        private void OnIconPackEntryClicked(object sender, Bitmap bitmap)
        {
            var eventArgs = new IconPackEntrySelectedEventArgs(_secret, bitmap);
            IconPackEntrySelected?.Invoke(this, eventArgs);
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

        public class IconPackEntrySelectedEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly Bitmap Icon;

            public IconPackEntrySelectedEventArgs(string secret, Bitmap icon)
            {
                Secret = secret;
                Icon = icon;
            }
        }
    }
}