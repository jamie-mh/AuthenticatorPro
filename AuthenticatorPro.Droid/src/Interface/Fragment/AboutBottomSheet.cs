// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    public class AboutBottomSheet : BottomSheet
    {
        public AboutBottomSheet() : base(Resource.Layout.sheetMenu, Resource.String.about)
        {
        }

        public event EventHandler AboutClicked;
        public event EventHandler SupportClicked;
        public event EventHandler RateClicked;
        public event EventHandler ViewGitHubClicked;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.outline_info_24, Resource.String.about, AboutClicked,
                        Resource.String.aboutSummary),
                    new(Resource.Drawable.ic_buymeacoffee, Resource.String.supportDevelopment, SupportClicked,
                        Resource.String.supportDevelopmentSummary),
                    new(Resource.Drawable.ic_googleplay, Resource.String.rate,
                        RateClicked, Resource.String.rateSummary),
                    new(Resource.Drawable.ic_github, Resource.String.viewGitHub,
                        ViewGitHubClicked, Resource.String.viewGitHubSummary)
                });

            return view;
        }
    }
}