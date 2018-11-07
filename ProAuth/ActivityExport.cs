using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using PCLCrypto;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using System.IO;
using System.Text;
using Android;
using Android.Content;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Permission = Android.Content.PM.Permission;
using Android.Runtime;
using ProAuth.Data;
using ProAuth.Utilities;
using SQLite;

namespace ProAuth
{
    [Activity(Label = "ExportActivity")]
    public class ActivityExport: AppCompatActivity
    {
        private const int PermissionStorageCode = 0;
        private const int FileSavePathCode = 1;

        private EditText _textPassword;
        private SQLiteAsyncConnection _connection;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityExport);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityExport_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.export);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            _textPassword = FindViewById<EditText>(Resource.Id.activityExport_password);
            Button exportBtn = FindViewById<Button>(Resource.Id.activityExport_export);
            exportBtn.Click += ExportButtonClick;

            _connection = await Database.Connect();
        }

        protected override void OnDestroy()
        {
            _connection.CloseAsync();
            base.OnDestroy();
        }

        private async void ExportButtonClick(object sender, EventArgs e)
        {
            int count = await _connection.Table<Authenticator>().CountAsync();

            if(count == 0)
            {
                Toast.MakeText(this, Resource.String.noAuthenticators, ToastLength.Short).Show();
                return;
            }

            if(!GetStoragePermission())
            {
                return;
            }

            string password = _textPassword.Text;
            Intent intent = new Intent(this, typeof(ActivityFileSave));
            intent.PutExtra("filename", $@"backup-{DateTime.Now:yyyy-MM-dd}");

            if(password == "")
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle(Resource.String.warning); 
                builder.SetMessage(Resource.String.confirmEmptyPassword);
                builder.SetNegativeButton(Resource.String.cancel, (s, args) => { });
                builder.SetPositiveButton(Resource.String.ok, (s, args) =>
                {
                    StartActivityForResult(intent, FileSavePathCode);
                });
                builder.SetCancelable(true);

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }
            else
            {
                StartActivityForResult(intent, FileSavePathCode);
            }
        }

        private bool GetStoragePermission()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage)
               != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this, 
                    new[] { Manifest.Permission.WriteExternalStorage }, PermissionStorageCode);
                return false;
            }

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    ExportButtonClick(null, null);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
                }
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(requestCode != FileSavePathCode || resultCode != Result.Ok)
                return;

            List<Authenticator> authenticators = await
                _connection.QueryAsync<Authenticator>("SELECT * FROM authenticator");

            string json = JsonConvert.SerializeObject(authenticators);
            string filename = intent.GetStringExtra("filename") + ".proauth";

            string path = Path.Combine(intent.GetStringExtra("path"), filename);
            byte[] dataToWrite;
            string password = _textPassword.Text;

            if(password != "")
            {
                SHA256 sha256 = SHA256.Create();
                byte[] keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                byte[] data = Encoding.UTF8.GetBytes(json);

                ISymmetricKeyAlgorithmProvider provider = 
                    WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);

                ICryptographicKey key = provider.CreateSymmetricKey(keyMaterial);

                dataToWrite = WinRTCrypto.CryptographicEngine.Encrypt(key, data);
            }
            else
            {
                dataToWrite = Encoding.UTF8.GetBytes(json);
            }

            File.WriteAllBytes(path, dataToWrite);
            Toast.MakeText(this, $@"Saved to storage as ""{filename}"".", ToastLength.Long).Show();

            Finish();
            base.OnActivityResult(requestCode, resultCode, intent);
        }

        public override bool OnSupportNavigateUp()
        {
            Finish();
            return base.OnSupportNavigateUp();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if(item.ItemId == Android.Resource.Id.Home)
            {
                Finish();
                return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            Finish();
            base.OnBackPressed();
        }
    }
}