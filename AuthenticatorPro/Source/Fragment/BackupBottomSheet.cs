using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.List;
using Google.Android.Material.Dialog;

namespace AuthenticatorPro.Fragment
{
    internal class BackupBottomSheet: BottomSheet
    {
        public event EventHandler ClickBackupFile;
        public event EventHandler ClickHtmlFile;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMenu, container, false);
            SetupToolbar(view, Resource.String.backup);
        
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_action_file_lock, Resource.String.backupToFile, ClickBackupFile, Resource.String.backupToFileMessage),
                new(Resource.Drawable.ic_action_code, Resource.String.backupHtml, OnHtmlClick, Resource.String.backupHtmlMessage)
            });
        
            return view;
        }

        private void OnHtmlClick(object sender, EventArgs e)
        {
            var builder = new MaterialAlertDialogBuilder(Activity);
            builder.SetTitle(Resource.String.warning);
            builder.SetMessage(Resource.String.backupHtmlWarning);
            builder.SetCancelable(true);

            builder.SetNegativeButton(Resource.String.cancel, delegate { });
            builder.SetPositiveButton(Resource.String.ok, delegate
            {
                ClickHtmlFile.Invoke(this, e);
            });

            builder.Create().Show();
        }
    }
}