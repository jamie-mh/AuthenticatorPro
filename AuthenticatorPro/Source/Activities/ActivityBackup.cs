using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Utilities;
using Java.IO;
using Newtonsoft.Json;
using PCLCrypto;
using SQLite;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using File = System.IO.File;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "BackupActivity")]
    public class ActivityBackup : AppCompatActivity
    {
        private const int PermissionStorageCode = 0;
        private const int DeviceStorageCode = 1;
        private const int StorageAccessFrameworkCode = 2;

        private SQLiteAsyncConnection _connection;

        private EditText _textPassword;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityBackup);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityBackup_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.backup);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            _textPassword = FindViewById<EditText>(Resource.Id.activityBackup_password);

            var saveStorageBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveStorage);
            saveStorageBtn.Click += SaveStorageClick;

            var saveCloudBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveCloud);
            saveCloudBtn.Click += SaveCloudClick;

            _connection = await Database.Connect();
        }

        protected override void OnDestroy()
        {
            _connection.CloseAsync();
            base.OnDestroy();
        }

        private async void SaveStorageClick(object sender, EventArgs e)
        {
            var count = await _connection.Table<Authenticator>().CountAsync();

            if(count == 0)
            {
                Toast.MakeText(this, Resource.String.noAuthenticators, ToastLength.Short).Show();
                return;
            }

            if(!GetStoragePermission()) return;

            var intent = new Intent(this, typeof(ActivityFile));
            intent.PutExtra("filename", $@"backup-{DateTime.Now:yyyy-MM-dd}");

            if(_textPassword.Text == "")
            {
                ShowPasswordDialog(intent, DeviceStorageCode);
            }
            else
            {
                StartActivityForResult(intent, DeviceStorageCode);
            }
        }

        private async void SaveCloudClick(object sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, $@"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

            if(_textPassword.Text == "")
            {
                ShowPasswordDialog(intent, StorageAccessFrameworkCode);
            }
            else
            {
                StartActivityForResult(intent, StorageAccessFrameworkCode);
            }
        }

        private void ShowPasswordDialog(Intent intent, int code)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.warning);
            builder.SetMessage(Resource.String.confirmEmptyPassword);
            builder.SetNegativeButton(Resource.String.cancel, (s, args) => { });
            builder.SetPositiveButton(Resource.String.ok,
                (s, args) => { StartActivityForResult(intent, code); });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        private bool GetStoragePermission()
        {
            if(ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage)
               != Permission.Granted)
            {
                ActivityCompat.RequestPermissions(this,
                    new[] {Manifest.Permission.WriteExternalStorage}, PermissionStorageCode);
                return false;
            }

            return true;
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    SaveStorageClick(null, null);
                else
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode,
            Intent intent)
        {
            if(resultCode != Result.Ok || (requestCode != DeviceStorageCode && requestCode != StorageAccessFrameworkCode))
                return;

            var backup = new Backup {
                Authenticators = await
                    _connection.QueryAsync<Authenticator>("SELECT * FROM authenticator"),

                Categories = await
                    _connection.QueryAsync<Category>("SELECT * FROM category"),

                AuthenticatorCategories = await
                    _connection.QueryAsync<AuthenticatorCategory>("SELECT * FROM authenticatorcategory")
            };

            var json = JsonConvert.SerializeObject(backup);
            byte[] dataToWrite;
            var password = _textPassword.Text;

            if(password != "")
            {
                var sha256 = SHA256.Create();
                var keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var data = Encoding.UTF8.GetBytes(json);

                var provider =
                    WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithm.AesCbcPkcs7);

                var key = provider.CreateSymmetricKey(keyMaterial);

                dataToWrite = WinRTCrypto.CryptographicEngine.Encrypt(key, data);
            }
            else
            {
                dataToWrite = Encoding.UTF8.GetBytes(json);
            }

            switch(requestCode)
            {
                case DeviceStorageCode:
                    var filename = intent.GetStringExtra("filename") + ".authpro";
                    var path = Path.Combine(intent.GetStringExtra("path"), filename);

                    File.WriteAllBytes(path, dataToWrite);
                    break;

                case StorageAccessFrameworkCode:
                    var output = ContentResolver.OpenOutputStream(intent.Data);

                    // Use Java streams, because a bug in Xamarin creates 0 byte files
                    //var writer = new BufferedWriter(new OutputStreamWriter(output));
                    var dataStream = new DataOutputStream(output);

                    foreach(var b in dataToWrite)
                        dataStream.Write(b);
    
                    dataStream.Flush();
                    dataStream.Close();
                    break;
            }

            Toast.MakeText(this, GetString(Resource.String.saveSuccess), ToastLength.Long).Show();
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