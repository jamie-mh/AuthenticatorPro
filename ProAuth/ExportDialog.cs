using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Newtonsoft.Json;
using OtpSharp;
using PCLCrypto;
using ProAuth.Data;
using ProAuth.Utilities;
using Environment = Android.OS.Environment;

namespace ProAuth
{
    class ExportDialog : DialogFragment
    {
        private AlertDialog _dialog;
        private Database _database;
        private string _password;
        private EditText _fileNameText;

        public ExportDialog(Database database, string password)
        {
            _database = database;
            _password = password;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.export);

            alert.SetPositiveButton(Resource.String.export, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogExport, null);
            _fileNameText = view.FindViewById<EditText>(Resource.Id.dialogExport_fileName);
            alert.SetView(view);

            _dialog = alert.Create();
            _dialog.Show();

            _fileNameText.Text = $@"backup-{DateTime.Now:yyyy-MM-dd}";

            // Button listeners
            Button exportButton = _dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = _dialog.GetButton((int) DialogButtonType.Negative);

            exportButton.Click += ExportClick;
            cancelButton.Click += CancelClick;

            return _dialog;
        }

        private void ExportClick(object sender, EventArgs e)
        {
            if(_fileNameText.Text.Trim() == "")
            {
                Toast.MakeText(_dialog.Context, Resource.String.noFileName, ToastLength.Short).Show();
                return;
            }

            List<Authenticator> authenticators = 
                _database.Connection.Query<Authenticator>("SELECT * FROM authenticator");

            string json = JsonConvert.SerializeObject(authenticators);
            string filename = _fileNameText.Text + ".proauth";

            string path = Path.Combine(Environment.ExternalStorageDirectory.AbsolutePath, filename);
            SHA256 sha256 = SHA256.Create();
            byte[] keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(_password));
            byte[] data = Encoding.UTF8.GetBytes(json);

            var provider = WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            byte[] cipherText = WinRTCrypto.CryptographicEngine.Encrypt(key, data, null);

            File.WriteAllBytes(path, cipherText);
            Toast.MakeText(_dialog.Context, $@"Saved to storage as ""{filename}"".", ToastLength.Long).Show();

            _dialog?.Dismiss();
        }

        private void CancelClick(object sender, EventArgs e)
        {
            _dialog?.Dismiss();
        }
    }
}