using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialogs;
using Java.IO;
using Newtonsoft.Json;
using PCLCrypto;
using SQLite;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using File = System.IO.File;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "BackupActivity")]
    public class BackupActivity : InternalStorageActivity
    {
        private const int DeviceStorageCode = 1;
        private const int StorageAccessFrameworkCode = 2;

        private SQLiteAsyncConnection _connection;
        private BackupPasswordDialog _passwordDialog;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            AuthenticatorPro.Theme.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityBackup);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityBackup_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.backup);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            var saveStorageBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveStorage);
            saveStorageBtn.Click += SaveStorageClick;

            var saveCloudBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveCloud);
            saveCloudBtn.Click += SaveCloudClick;

            _connection = await Database.Connect(this);
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

            if(!GetStoragePermission())
                return;

            var intent = new Intent(this, typeof(FileActivity));
            intent.PutExtra("filename", $@"backup-{DateTime.Now:yyyy-MM-dd}");

            StartActivityForResult(intent, DeviceStorageCode);
        }

        protected override void OnStoragePermissionGranted()
        {
            SaveStorageClick(null, null);
        }

        private async void SaveCloudClick(object sender, EventArgs e)
        {
            var count = await _connection.Table<Authenticator>().CountAsync();

            if(count == 0)
            {
                Toast.MakeText(this, Resource.String.noAuthenticators, ToastLength.Short).Show();
                return;
            }

            var intent = new Intent(Intent.ActionCreateDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");
            intent.PutExtra(Intent.ExtraTitle, $@"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

            StartActivityForResult(intent, StorageAccessFrameworkCode);
        }

        private void ShowPasswordDialog(int requestCode, Intent intent)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("password_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _passwordDialog = new BackupPasswordDialog(BackupPasswordDialog.Mode.Backup, () =>
            {
                Backup(requestCode, intent);
            }, null);

            _passwordDialog.Show(transaction, "password_dialog");
        }

        private void ShowNoPasswordDialog(int requestCode, Intent intent)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.warning);
            builder.SetMessage(Resource.String.confirmEmptyPassword);
            builder.SetNegativeButton(Resource.String.no, (s, args) => { });
            builder.SetPositiveButton(Resource.String.yes, (s, args) => 
            {
                _passwordDialog.Dismiss();
                Backup(requestCode, intent, true);
            });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(resultCode != Result.Ok || (requestCode != DeviceStorageCode && requestCode != StorageAccessFrameworkCode))
                return;

            ShowPasswordDialog(requestCode, intent);
            base.OnActivityResult(requestCode, resultCode, intent);
        }

        private async void Backup(int requestCode, Intent intent, bool confirmNoPassword = false)
        {
            var password = _passwordDialog.Password;

            if(!confirmNoPassword && password == "")
            {
                ShowNoPasswordDialog(requestCode, intent);
                return;
            }

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