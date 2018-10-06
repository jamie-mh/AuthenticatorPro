using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ProAuth.Utilities;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using ProAuth.Data;
using Newtonsoft.Json;
using System.IO;
using Environment = Android.OS.Environment;
using PCLCrypto;
using System.Security.Cryptography;

namespace ProAuth
{
    [Activity(Label = "ExportActivity")]
    public class ExportActivity: AppCompatActivity
    {
        private Database _database;
        private EditText _textPassword;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityExport);
            _database = new Database(this);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityExport_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.export);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _textPassword = FindViewById<EditText>(Resource.Id.activityExport_password);
            Button exportBtn = FindViewById<Button>(Resource.Id.activityExport_export);
            exportBtn.Click += ExportClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _database?.Connection.Close();
        }

        private void ExportClick(object sender, EventArgs e)
        {
            FragmentTransaction transaction = FragmentManager.BeginTransaction();
            Fragment old = FragmentManager.FindFragmentByTag("export_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            ExportDialog fragment = new ExportDialog(_database, _textPassword.Text) {
                Arguments = null
            };

            fragment.Show(transaction, "export_dialog");
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId) 
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
            }
            return base.OnOptionsItemSelected (item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}