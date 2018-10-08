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
using Plugin.FilePicker;
using Plugin.FilePicker.Abstractions;
using System.Text;
using ProAuth.Data;

namespace ProAuth
{
    [Activity(Label = "ImportActivity")]
    public class ImportActivity: AppCompatActivity
    {
        private Database _database;
        private FileData _file;
        private ImportDialog _dialog;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityImport);
            _database = new Database(this);

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
            try
            {
                _file = await CrossFilePicker.Current.PickFile();

                if(_file == null)
                {
                    return;
                }

                FragmentTransaction transaction = FragmentManager.BeginTransaction();
                Fragment old = FragmentManager.FindFragmentByTag("import_dialog");

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

        private void OnDialogPositive(object sender, EventArgs e)
        {
            try
            {
                SHA256 sha256 = SHA256.Create();
                byte[] password = Encoding.UTF8.GetBytes(_dialog.Password);
                byte[] keyMaterial = sha256.ComputeHash(password);

                ISymmetricKeyAlgorithmProvider provider = 
                    WinRTCrypto.SymmetricKeyAlgorithmProvider.OpenAlgorithm(PCLCrypto.SymmetricAlgorithm.AesCbcPkcs7);

                ICryptographicKey key = provider.CreateSymmetricKey(keyMaterial);

                byte[] data = _file.DataArray;
                byte[] raw = WinRTCrypto.CryptographicEngine.Decrypt(key, data);
                string contents = Encoding.UTF8.GetString(raw);

                List<Authenticator> auths = JsonConvert.DeserializeObject<List<Authenticator>>(contents);
                auths.ForEach((a) => _database.Connection.Insert(a));
                Toast.MakeText(_dialog.Context, $@"Imported {auths.Count} authenticator(s).", ToastLength.Long).Show();

                _dialog.Dismiss();
            }
            catch
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