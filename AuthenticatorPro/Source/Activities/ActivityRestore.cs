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
using AuthenticatorPro.Dialogs;
using AuthenticatorPro.Utilities;
using AuthenticatorPro.Utilities.AuthenticatorList;
using AuthenticatorPro.Utilities.CategoryList;
using Newtonsoft.Json;
using PCLCrypto;
using SQLite;
using SymmetricAlgorithm = PCLCrypto.SymmetricAlgorithm;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace AuthenticatorPro.Activities
{
    [Activity(Label = "RestoreActivity")]
    public class ActivityRestore : AppCompatActivity
    {
        private const int PermissionStorageCode = 0;
        private const int FilePathCode = 1;
        private AuthSource _authSource;
        private CategorySource _categorySource;

        private SQLiteAsyncConnection _connection;
        private DialogRestore _dialog;

        private byte[] _fileData;

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityRestore);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityRestore_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.restore);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Icons.GetIcon("arrow_back"));

            var importBtn = FindViewById<Button>(Resource.Id.activityRestore_import);
            importBtn.Click += ImportButtonClick;

            _connection = await Database.Connect();
            _authSource = new AuthSource(_connection);
            _categorySource = new CategorySource(_connection);
        }

        protected override void OnDestroy()
        {
            _connection.CloseAsync();
            base.OnDestroy();
        }

        private async void ImportButtonClick(object sender, EventArgs e)
        {
            if(!GetStoragePermission()) return;

            try
            {
                var intent = new Intent(this, typeof(ActivityFile));
                intent.PutExtra("mode", (int) ActivityFile.Mode.Open);
                StartActivityForResult(intent, FilePathCode);
            }
            catch
            {
                Toast.MakeText(this, Resource.String.filePickError, ToastLength.Short).Show();
            }
        }

        protected override async void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode,
            Intent intent)
        {
            if(requestCode != FilePathCode || resultCode != Result.Ok)
                return;

            var file = $@"{intent.GetStringExtra("path")}/{intent.GetStringExtra("filename")}";
            _fileData = File.ReadAllBytes(file);

            if(_fileData.Length == 0)
            {
                Toast.MakeText(this, Resource.String.invalidFileError, ToastLength.Short).Show();
                return;
            }

            // Open curly brace (file is not encrypted)
            if(_fileData[0] == 0x7b)
            {
                RestoreBackup();
                return;
            }

            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("import_dialog");

            if(old != null) transaction.Remove(old);

            transaction.AddToBackStack(null);
            _dialog = new DialogRestore(OnDialogPositive, OnDialogNegative);
            _dialog.Show(transaction, "import_dialog");
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions,
            [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                    ImportButtonClick(null, null);
                else
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
            }

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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

        private async void RestoreBackup(string password = "")
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
                var authsInserted = 0;
                var categoriesInserted = 0;

                foreach(var auth in backup.Authenticators)
                {
                    if(_authSource.IsDuplicate(auth)) continue;

                    await _connection.InsertAsync(auth);
                    authsInserted++;
                }

                foreach(var category in backup.Categories)
                {
                    if(_categorySource.IsDuplicate(category)) continue;

                    await _connection.InsertAsync(category);
                    categoriesInserted++;
                }

                foreach(var binding in backup.AuthenticatorCategories)
                {
                    if(_authSource.CategoryBindings.Contains(binding)) continue;

                    await _connection.InsertAsync(binding);
                }

                var message = String.Format(GetString(Resource.String.restoredFromBackup), authsInserted,
                    categoriesInserted);
                Toast.MakeText(this, message, ToastLength.Long).Show();

                Finish();
            }
            catch
            {
                Toast.MakeText(_dialog.Context, Resource.String.restoreError, ToastLength.Long).Show();
            }
        }

        private void OnDialogPositive(object sender, EventArgs e)
        {
            RestoreBackup(_dialog.Password);
        }

        private void OnDialogNegative(object sender, EventArgs e)
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