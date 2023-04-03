// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Interface.Adapter;
using AuthenticatorPro.Droid.Interface.LayoutManager;
using AuthenticatorPro.Droid.Persistence.View;
using Google.Android.Material.Button;
using System;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class ChangeIconBottomSheet : BottomSheet
    {
        public event EventHandler<IconSelectedEventArgs> IconSelected;
        public event EventHandler UseCustomIconClick;

        private readonly IIconView _iconView;
        private string _secret;

        private IconListAdapter _iconListAdapter;
        private RecyclerView _iconList;
        private EditText _searchText;

        public ChangeIconBottomSheet() : base(Resource.Layout.sheetChangeIcon)
        {
            _iconView = Dependencies.Resolve<IIconView>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _secret = Arguments.GetString("secret");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.changeIcon, true);

            _searchText = view.FindViewById<EditText>(Resource.Id.editSearch);
            _iconList = view.FindViewById<RecyclerView>(Resource.Id.list);

            _searchText.TextChanged += OnSearchChanged;

            var customIconButton = view.FindViewById<MaterialButton>(Resource.Id.buttonUseCustomIcon);
            customIconButton.Click += (s, e) =>
            {
                UseCustomIconClick?.Invoke(s, e);
                Dismiss();
            };

            var baseActivity = (BaseActivity) Context;
            _iconView.UseDarkTheme = baseActivity.IsDark;
            _iconView.Update();

            _iconListAdapter = new IconListAdapter(Context, _iconView);
            _iconListAdapter.ItemClicked += OnItemClicked;
            _iconListAdapter.HasStableIds = true;

            _iconList.SetAdapter(_iconListAdapter);
            _iconList.HasFixedSize = true;
            _iconList.SetItemViewCacheSize(20);
            _iconList.SetItemAnimator(null);

            var layout = new AutoGridLayoutManager(Context, 140);
            _iconList.SetLayoutManager(layout);

            return view;
        }

        private void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            _iconView.Search = e.Text.ToString();
            _iconListAdapter.NotifyDataSetChanged();
        }

        private void OnItemClicked(object sender, int iconPosition)
        {
            var eventArgs = new IconSelectedEventArgs(_secret, _iconView[iconPosition].Key);
            IconSelected?.Invoke(this, eventArgs);
        }

        public class IconSelectedEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly string Icon;

            public IconSelectedEventArgs(string secret, string icon)
            {
                Secret = secret;
                Icon = icon;
            }
        }
    }
}