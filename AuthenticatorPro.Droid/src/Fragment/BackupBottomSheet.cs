// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using Google.Android.Material.Dialog;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class BackupBottomSheet : BottomSheet
    {
        public event EventHandler BackupFileClicked;
        public event EventHandler BackupHtmlFileClicked;
        public event EventHandler BackupUriListClicked;

        public BackupBottomSheet() : base(Resource.Layout.sheetMenu) { }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.backup);

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu,
                new List<SheetMenuItem>
                {
                    new(Resource.Drawable.ic_lock, Resource.String.backupToFile,
                        BackupFileClicked, Resource.String.backupToFileMessage),
                    new(Resource.Drawable.ic_html, Resource.String.backupHtml,
                        delegate { ShowUnencryptedWarning(Resource.String.backupHtmlWarning, BackupHtmlFileClicked); },
                        Resource.String.backupHtmlMessage),
                    new(Resource.Drawable.ic_list, Resource.String.backupUriList,
                        delegate { ShowUnencryptedWarning(Resource.String.backupUriListWarning, BackupUriListClicked); },
                        Resource.String.backupUriListMessage)
                });

            return view;
        }

        private void ShowUnencryptedWarning(int warningRes, EventHandler onContinue)
        {
            var builder = new MaterialAlertDialogBuilder(Activity);
            builder.SetTitle(Resource.String.warning);
            builder.SetMessage(warningRes);
            builder.SetCancelable(true);

            builder.SetNegativeButton(Resource.String.cancel, delegate { });
            builder.SetPositiveButton(Resource.String.ok, delegate
            {
                onContinue.Invoke(this, EventArgs.Empty);
            });

            builder.Create().Show();
        }
    }
}