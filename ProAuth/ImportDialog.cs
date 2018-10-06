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
    class ImportDialog : DialogFragment
    {
        private AlertDialog _dialog;
        private Database _database;
        private byte[] _data;
        private EditText _passwordText;

        public ImportDialog(Database database, byte[] data)
        {
            _database = database;
            _data = data;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.importString);

            alert.SetPositiveButton(Resource.String.importString, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogImport, null);
            _passwordText = view.FindViewById<EditText>(Resource.Id.dialogImport_password);
            alert.SetView(view);

            _dialog = alert.Create();
            _dialog.Show();

            // Button listeners
            Button importButton = _dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = _dialog.GetButton((int) DialogButtonType.Negative);

            importButton.Click += ImportClick;
            cancelButton.Click += CancelClick;

            return _dialog;
        }

        private void ImportClick(object sender, EventArgs e)
        {
            try
            {
                SHA256 sha256 = SHA256.Create();
                byte[] keyMaterial = sha256.ComputeHash(Encoding.UTF8.GetBytes(_passwordText.Text));

                var provider = WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);
                var key = provider.CreateSymmetricKey(keyMaterial);

                byte[] raw = WinRTCrypto.CryptographicEngine.Decrypt(key, _data, null);
                string contents = Encoding.UTF8.GetString(raw);

                List<Authenticator> auths = JsonConvert.DeserializeObject<List<Authenticator>>(contents);
                auths.ForEach((a) => _database.Connection.Insert(a));
                Toast.MakeText(_dialog.Context, $@"Imported {auths.Count} authenticator(s).", ToastLength.Long).Show();

                _dialog?.Dismiss();
            }
            catch
            {
                Toast.MakeText(_dialog.Context, Resource.String.importError, ToastLength.Long).Show();
            }
        }

        private void CancelClick(object sender, EventArgs e)
        {
            _dialog?.Dismiss();
        }

        public override int Show(FragmentTransaction transaction, string tag)
        {
            try
            {
                transaction.Add(this, tag).AddToBackStack(null);
                transaction.CommitAllowingStateLoss();

                return 1;
            }
            catch(Java.Lang.IllegalStateException ex)
            {
                throw ex;
            }
        }
    }
}