using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using ProAuth.Utilities;
using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using PCLCrypto;
using Toolbar = Android.Support.V7.Widget.Toolbar;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using ProAuth.Data;
using System.IO;
using Environment = Android.OS.Environment;
using System.Text;
using Android;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Fragment = Android.Support.V4.App.Fragment;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using Permission = Android.Content.PM.Permission;
using Android.Content.PM;
using Android.Runtime;

namespace ProAuth
{
    [Activity(Label = "ExportActivity")]
    public class ExportActivity: AppCompatActivity
    {
        private const int PermissionStorageCode = 0;

        private Database _database;
        private EditText _textPassword;
        private ExportDialog _dialog;    

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
            exportBtn.Click += this.ExportButtonClick;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _database?.Connection.Close();
        }

        private void ExportButtonClick(object sender, EventArgs e)
        {
            string password = _textPassword.Text;

            if(password == "")
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetTitle(Resource.String.warning); 
                builder.SetMessage(Resource.String.confirmEmptyPassword);
                builder.SetNegativeButton(Resource.String.cancel, (s, args) => { });
                builder.SetPositiveButton(Resource.String.ok, (s, args) => { ShowExportDialog(); });
                builder.SetCancelable(true);

                AlertDialog dialog = builder.Create();
                dialog.Show();
            }
            else
            {
                ShowExportDialog();
            }
        }

        private void OnDialogPositive(object sender, EventArgs e)
        {
            if(!GetStoragePermission())
            {
                return;
            }

            if(_dialog.FileName.Trim() == "")
            {
                Toast.MakeText(_dialog.Context, Resource.String.noFileName, ToastLength.Short).Show();
                return;
            }

            List<Authenticator> authenticators = 
                _database.Connection.Query<Authenticator>("SELECT * FROM authenticator");

            string json = JsonConvert.SerializeObject(authenticators);
            string filename = _dialog.FileName + ".proauth";

            string path = Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, filename);
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
            Toast.MakeText(_dialog.Context, $@"Saved to storage as ""{filename}"".", ToastLength.Long).Show();

            _dialog?.Dismiss();
        }

        private void OnDialogNegative(object sender, EventArgs e)
        {
            _dialog.Dismiss();
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
                    OnDialogPositive(null, null);
                }
                else
                {
                    Toast.MakeText(this, Resource.String.externalStoragePermissionError, ToastLength.Short).Show();
                }
            }
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        private void ShowExportDialog()
        {
            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            Fragment old = SupportFragmentManager.FindFragmentByTag("export_dialog");

            if(old != null)
            {
                transaction.Remove(old);
            }

            transaction.AddToBackStack(null);
            _dialog = new ExportDialog(OnDialogPositive, OnDialogNegative);
            _dialog.Show(transaction, "export_dialog");
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