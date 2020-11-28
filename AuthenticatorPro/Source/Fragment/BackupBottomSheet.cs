using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class BackupBottomSheet: BottomSheet
    {
        public event EventHandler ClickBackupFile;
        public event EventHandler ClickHtmlFile;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetBackup, container, false);
        
            var fileItem = view.FindViewById<LinearLayout>(Resource.Id.buttonFile);
            var htmlItem = view.FindViewById<LinearLayout>(Resource.Id.buttonHtml);
        
            fileItem.Click += (sender, e) => {
                ClickBackupFile?.Invoke(sender, e);
                Dismiss();
            };
        
            htmlItem.Click += (sender, e) => {
                ClickHtmlFile?.Invoke(sender, e);
                Dismiss();
            };
        
            return view;
        }
    }
}