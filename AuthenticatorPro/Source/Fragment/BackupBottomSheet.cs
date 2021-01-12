using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.List;

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
                new(Resource.Drawable.ic_action_phonelink_lock, Resource.String.backupToFile, ClickBackupFile, Resource.String.backupToFileMessage),
                new(Resource.Drawable.ic_action_code, Resource.String.backupHtml, ClickHtmlFile, Resource.String.backupHtmlMessage)
            });
        
            return view;
        }
    }
}