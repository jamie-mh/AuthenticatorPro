// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.List;
using AuthenticatorPro.Droid.Shared.Data;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AboutBottomSheet : BottomSheet
    {
        public event EventHandler ClickAbout;
        public event EventHandler ClickRate;
        public event EventHandler ClickViewGitHub;
        
        public AboutBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            var isDark = ((BaseActivity) Context).IsDark; 

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new SheetMenuItem(Resource.Drawable.ic_action_info_outline, Resource.String.about, ClickAbout, Resource.String.aboutSummary),
                new SheetMenuItem(IconResolver.GetService("googleplay", isDark), Resource.String.rate, ClickRate, Resource.String.rateSummary),
                new SheetMenuItem(IconResolver.GetService("github", isDark), Resource.String.viewGitHub, ClickViewGitHub, Resource.String.viewGitHubSummary)
            });

            return view;
        }
    }
}