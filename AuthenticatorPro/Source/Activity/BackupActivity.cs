using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Dialog;
using Java.IO;
using SQLite;
using AlertDialog = Android.Support.V7.App.AlertDialog;
using File = System.IO.File;
using Toolbar = AndroidX.AppCompat.Widget.Toolbar;


namespace AuthenticatorPro.Activity
{
    [Activity]
    internal class BackupActivity : InternalStorageActivity
    {
        private const int DeviceStorageCode = 1;
        private const int StorageAccessFrameworkCode = 2;

        private SQLiteAsyncConnection _connection;
        private AuthenticatorSource _authenticatorSource;
        private CategorySource _categorySource;

        private BackupPasswordDialog _passwordDialog;


        protected override async void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.activityBackup);

            var toolbar = FindViewById<Toolbar>(Resource.Id.activityBackup_toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetTitle(Resource.String.backup);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_action_arrow_back);

            var saveStorageBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveStorage);
            saveStorageBtn.Click += OnSaveStorageClick;

            var saveCloudBtn = FindViewById<LinearLayout>(Resource.Id.activityBackup_saveCloud);
            saveCloudBtn.Click += OnSaveCloudClick;

            _connection = await Database.Connect(this);
            _authenticatorSource = new AuthenticatorSource(_connection);
            _categorySource = new CategorySource(_connection);
        }

        protected override async void OnDestroy()
        {
            base.OnDestroy();
            await _connection.CloseAsync();
        }

        private async void OnSaveStorageClick(object sender = null, EventArgs e = null)
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
            intent.PutExtra("filename", $"backup-{DateTime.Now:yyyy-MM-dd}");

            StartActivityForResult(intent, DeviceStorageCode);
        }

        protected override void OnStoragePermissionGranted()
        {
            OnSaveStorageClick();
        }

        private async void OnSaveCloudClick(object sender, EventArgs e)
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
            intent.PutExtra(Intent.ExtraTitle, $"backup-{DateTime.Now:yyyy-MM-dd}.authpro");

            StartActivityForResult(intent, StorageAccessFrameworkCode);
        }

        private void ShowPasswordDialog(int requestCode, Intent intent)
        {
            var transaction = SupportFragmentManager.BeginTransaction();
            var old = SupportFragmentManager.FindFragmentByTag("password_dialog");

            if(old != null)
                transaction.Remove(old);

            transaction.AddToBackStack(null);

            _passwordDialog = new BackupPasswordDialog(BackupPasswordDialog.Mode.Backup);
            _passwordDialog.PasswordEntered += async (sender, password) =>
            {
                await Backup(requestCode, intent, password);
            };
            _passwordDialog.Show(transaction, "password_dialog");
        }

        private void ShowNoPasswordConfirmDialog(int requestCode, Intent intent)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.warning);
            builder.SetMessage(Resource.String.confirmEmptyPassword);
            builder.SetNegativeButton(Resource.String.no, (s, args) => { });
            builder.SetPositiveButton(Resource.String.yes, async (s, args) => 
            {
                _passwordDialog.Dismiss();
                await Backup(requestCode, intent, null, true);
            });
            builder.SetCancelable(true);

            var dialog = builder.Create();
            dialog.Show();
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent intent)
        {
            if(resultCode != Result.Ok || requestCode != DeviceStorageCode && requestCode != StorageAccessFrameworkCode)
                return;

            ShowPasswordDialog(requestCode, intent);
            base.OnActivityResult(requestCode, resultCode, intent);
        }

        private async Task Backup(int requestCode, Intent intent, string password, bool confirmNoPassword = false)
        {
            if(!confirmNoPassword && String.IsNullOrEmpty(password))
            {
                ShowNoPasswordConfirmDialog(requestCode, intent);
                return;
            }

            await _authenticatorSource.Update();
            await _categorySource.Update();

            var backup = new Backup(
                _authenticatorSource.Authenticators,
                _categorySource.Categories,
                _authenticatorSource.CategoryBindings
            );

            var dataToWrite = backup.ToBytes(password);

            switch(requestCode)
            {
                case DeviceStorageCode:
                    var filename = intent.GetStringExtra("filename") + ".authpro";
                    var path = Path.Combine(intent.GetStringExtra("path"), filename);

                    await File.WriteAllBytesAsync(path, dataToWrite);
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