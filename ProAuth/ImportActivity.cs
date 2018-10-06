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
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System.Security.Cryptography;
using PCLCrypto;

namespace ProAuth
{
    [Activity(Label = "ImportActivity")]
    public class ImportActivity: AppCompatActivity
    {
        private Database _database;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityImport);
            _database = new Database(this);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityImport_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.importString);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            Button importBtn = FindViewById<Button>(Resource.Id.activityImport_import);
            importBtn.Click += ImportClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _database?.Connection.Close();
        }

        private async void ImportClick(object sender, EventArgs e)
        {
            try
            {
                FileData fileData = await CrossFilePicker.Current.PickFile();
                if (fileData == null)
                    return; // user canceled file picking

                FragmentTransaction transaction = FragmentManager.BeginTransaction();
                Fragment old = FragmentManager.FindFragmentByTag("import_dialog");

                if(old != null)
                {
                    transaction.Remove(old);
                }

                transaction.AddToBackStack(null);
                ImportDialog fragment = new ImportDialog(_database, fileData.DataArray) {
                    Arguments = null
                };

                fragment.Show(transaction, "import_dialog");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Exception choosing file: " + ex.ToString());
            }
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