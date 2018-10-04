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
            List<Authenticator> authenticators = 
                _database.Connection.Query<Authenticator>("SELECT * FROM authenticator");

            string json = JsonConvert.SerializeObject(authenticators);

            string filename = $@"backup-{DateTime.Now:yyyy-MM-dd}.proauth";
            string path = Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, filename);

            //File.WriteAllText(path, json);
            //Toast.MakeText(this, $@"Saved to storage as ""{filename}"".", ToastLength.Long).Show();

            // use picker save file
            SHA256 sha256 = SHA256.Create();
            byte[] keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes("test"));
            byte[] data = Encoding.UTF8.GetBytes(json);

            var provider = WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            byte[] cipherText = WinRTCrypto.CryptographicEngine.Encrypt(key, data, null);

            File.WriteAllBytes(path, cipherText);
            Toast.MakeText(this, $@"Saved to storage as ""{filename}"".", ToastLength.Long).Show();
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