using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.List;
using Google.Android.Material.Dialog;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class BackupBottomSheet: BottomSheet
    {
        public event EventHandler ClickBackupFile;
        public event EventHandler ClickHtmlFile;
        public event EventHandler ClickUriList;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMenu, container, false);
            SetupToolbar(view, Resource.String.backup);
        
            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_action_file_lock, Resource.String.backupToFile, ClickBackupFile, Resource.String.backupToFileMessage),
                new(Resource.Drawable.ic_action_code, Resource.String.backupHtml, delegate { ShowUnencryptedWarning(Resource.String.backupHtmlWarning, ClickHtmlFile); }, Resource.String.backupHtmlMessage),
                new(Resource.Drawable.ic_list, Resource.String.backupUriList, delegate { ShowUnencryptedWarning(Resource.String.backupUriListWarning, ClickUriList); }, Resource.String.backupUriListMessage)
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