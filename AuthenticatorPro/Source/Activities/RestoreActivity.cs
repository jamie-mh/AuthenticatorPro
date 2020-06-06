using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialogs;
using Newtonsoft.Json;
using PCLCrypto;
using SQLite;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity]
    internal class RestoreActivity : InternalStorageActivity
    {
        private const int DeviceStorageCode = 1;
        private const int StorageAccessFrameworkCode = 2;

        private AuthenticatorSource _authenticatorSource;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private BackupPasswordDialog _dialog;

        private byte[] _fileData;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityRestore);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityRestore_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.restore);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            var loadStorageBtn = FindViewById<LinearLayout>(Resource.Id.activityRestore_loadStorage);
            loadStorageBtn.Click += LoadStorageClick;

            var loadCloudBtn = FindViewById<LinearLayout>(Resource.Id.activityRestore_loadCloud);
            loadCloudBtn.Click += LoadCloudClick;

            _connection = await Database.Connect(this);
            _authenticatorSource = new AuthenticatorSource(_connection);
            _categorySource = new CategorySource(_connection);
        }

        protected override void OnDestroy()
        {
            _connection.CloseAsync();
            base.OnDestroy();
        }

        private void LoadStorageClick(object sender, EventArgs e)
        {
            if(!GetStoragePermission())
                return;

            try
            {
                var intent = new Intent(this, typeof(FileActivity));
                intent.PutExtra("mode", (int) FileActivity.Mode.Open);
                StartActivityForResult(intent, DeviceStorageCode);
            }
            catch
            {
                Toast.MakeText(this, Resource.String.filePickError, ToastLength.Short).Show();
            }
        }

        protected override void OnStoragePermissionGranted()
        {
            LoadStorageClick(null, null);
        }

        private void LoadCloudClick(object sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionOpenDocument);
            intent.AddCategory(Intent.CategoryOpenable);
            intent.SetType("application/octet-stream");

            StartActivityForResult(intent, StorageAccessFrameworkCode);
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(resultCode != Result.Ok)
                return;

            switch(requestCode)
            {
                case DeviceStorageCode:
                    var file = $@"{intent.GetStringExtra("path")}/{intent.GetStringExtra("filename")}";
                    _fileData = await File.ReadAllBytesAsync(file);
                    break;

                case StorageAccessFrameworkCode:
                    var stream = ContentResolver.OpenInputStream(intent.Data);
                    var memoryStream = new MemoryStream();

                    await stream.CopyToAsync(memoryStream);
                    _fileData = memoryStream.ToArray();
                    break;

                default: return;
            }

            if(_fileData.Length == 0)
            {
                Toast.MakeText(this, Resource.String.invalidFileError, ToastLength.Short).Show();
                return;
            }

            // Open curly brace (file is not encrypted)
            if(_fileData[0] == 0x7b)
            {
                await Restore();
                return;
            }

            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("password_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _dialog = new BackupPasswordDialog(BackupPasswordDialog.Mode.Restore, OnDialogPositive, OnDialogNegative);
            _dialog.Show(transaction, "password_dialog");
        }

        private async Task Restore(string password = "")
        {
            try
            {
                string contents;

                if(String.IsNullOrEmpty(password))
                {
                    contents = Encoding.UTF8.GetString(_fileData);
                }
                else
                {
                    var sha256 = SHA256.Create();
                    var passwordBytes = Encoding.UTF8.GetBytes(password);
                    var keyMaterial = sha256.ComputeHash(passwordBytes);

                    var provider =
                        WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithm.AesCbcPkcs7);

                    var key = provider.CreateSymmetricKey(keyMaterial);

                    var raw = WinRTCrypto.CryptographicEngine.Decrypt(key, _fileData);
                    contents = Encoding.UTF8.GetString(raw);
                    _dialog.Dismiss();
                }

                var backup = JsonConvert.DeserializeObject<Backup>(contents);

                if(backup.Authenticators == null)
                {
                    Toast.MakeText(this, Resource.String.invalidFileError, ToastLength.Short).Show();
                    return;
                }

                var authsInserted = 0;
                var categoriesInserted = 0;

                foreach(var auth in backup.Authenticators.Where(auth => !_authenticatorSource.IsDuplicate(auth)))
                {
                    auth.Validate();
                    await _connection.InsertAsync(auth);
                    authsInserted++;
                }

                foreach(var category in backup.Categories.Where(category => !_categorySource.IsDuplicate(category)))
                {
                    await _connection.InsertAsync(category);
                    categoriesInserted++;
                }

                foreach(var binding in backup.AuthenticatorCategories.Where(binding => !_authenticatorSource.IsDuplicateCategoryBinding(binding)))
                {
                    await _connection.InsertAsync(binding);
                }

                var message = String.Format(GetString(Resource.String.restoredFromBackup), authsInserted,
                    categoriesInserted);
                Toast.MakeText(this, message, ToastLength.Long).Show();

                Finish();
            }
            catch(InvalidAuthenticatorException)
            {
                Toast.MakeText(this, Resource.String.invalidFileError, ToastLength.Short).Show();
            }
            catch
            {
                _dialog.Error = GetString(Resource.String.restoreError);
            }
        }

        private async void OnDialogPositive()
        {
            await Restore(_dialog.Password);
        }

        private void OnDialogNegative()
        {
            _dialog.Dismiss();
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