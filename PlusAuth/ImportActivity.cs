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
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System.Text;
using System.Text.RegularExpressions;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using PlusAuth.Data;
using PlusAuth.Utilities;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Android.Support.V7.Preferences;

namespace PlusAuth
{
    [Activity(Label = "ImportActivity")]
    public class ImportActivity: AppCompatActivity
    {
        private const int PermissionStorageCode = 0;

        private Database _database;
        private AuthSource _authSource;

        private FileData _file;
        private ImportDialog _dialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ThemeHelper.Update(this);
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityImport);

            _database = new Database();
            _authSource = new AuthSource(_database.Connection);

            Toolbar toolbar = FindViewById<Toolbar>(Resource.Id.activityImport_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.importString);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            Button importBtn = FindViewById<Button>(Resource.Id.activityImport_import);
            importBtn.Click += this.ImportButtonClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _database?.Connection.Close();
        }

        private async void ImportButtonClick(object sender, EventArgs e)
        {
            if(!GetStoragePermission())
            {
                return;
            }

            try
            {
                _file = await CrossFilePicker.Current.PickFile();

                if(_file == null)
                {
                    return;
                }

                Match filenameMatch = Regex.Match(_file.FileName, @"^(.*?)\.(.*?)$");

                if(filenameMatch.Success == false || filenameMatch.Groups.Count < 3 ||
                   filenameMatch.Groups[2].Value != "plusauth")
                {
                    Toast.MakeText(this, Resource.String.invalidFileError, ToastLength.Short).Show();
                    return;
                }

                FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
                Fragment old = SupportFragmentManager.FindFragmentByTag("import_dialog");

                if(old != null)
                {
                    transaction.Remove(old);
                }

                transaction.AddToBackStack(null);
                _dialog = new ImportDialog(OnDialogPositive, OnDialogNegative);
                _dialog.Show(transaction, "import_dialog");
            }
            catch
            {
                Toast.MakeText(this, Resource.String.filePickError, ToastLength.Short).Show();
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            if(requestCode == PermissionStorageCode)
            {
                if(grantResults.Length > 0 && grantResults[0] == Permission.Granted)
                {
                    ImportButtonClick(null, null);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
                }
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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

        private void OnDialogPositive(object sender, EventArgs e)
        {
            try
            {
                string contents = "";

                if(_dialog.Password == "")
                {
                    contents = Encoding.UTF8.GetString(_file.DataArray);
                }
                else
                {
                    SHA256 sha256 = SHA256.Create();
                    byte[] password = Encoding.UTF8.GetBytes(_dialog.Password);
                    byte[] keyMaterial = sha256.ComputeHash(password);

                    ISymmetricKeyAlgorithmProvider provider = 
                        WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);

                    ICryptographicKey key = provider.CreateSymmetricKey(keyMaterial);

                    byte[] data = _file.DataArray;
                    byte[] raw = WinRTCrypto.CryptographicEngine.Decrypt(key, data);
                    contents = Encoding.UTF8.GetString(raw);
                }

                List<Authenticator> auths = JsonConvert.DeserializeObject<List<Authenticator>>(contents);
                int inserted = 0;

                foreach(Authenticator auth in auths)
                {
                    if(_authSource.IsDuplicate(auth))
                    {
                        continue;
                    }

                    _database.Connection.Insert(auth);
                    inserted++;
                }

                string message = String.Format(GetString(Resource.String.importedNewAuthenticators), inserted);
                Toast.MakeText(_dialog.Context, message, ToastLength.Long).Show();

                _dialog.Dismiss();
                Finish();
            }
            catch(Exception ex)
            {
                Toast.MakeText(_dialog.Context, Resource.String.importError, ToastLength.Long).Show();
            }
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
                this.Finish();
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