using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.Preference;
using AndroidX.Work;
using AuthenticatorPro.Activity;
using AuthenticatorPro.Worker;
using Google.Android.Material.Button;
using Google.Android.Material.SwitchMaterial;
using Java.Util.Concurrent;
using Xamarin.Essentials;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Fragment
{
    internal class AutoBackupSetupBottomSheet : BottomSheet
    {
        private const int RequestPicker = 0;

        private TextView _locationStatusText;
        private TextView _passwordStatusText;

        private MaterialButton _okButton;
        private SwitchMaterial _enabledSwitch;
        private MaterialButton _testBackupButton;

        private string _backupLocationUri;
        private bool? _hasPassword;
        
        public AutoBackupSetupBottomSheet()
        {
            RetainInstance = true;
        }

        public override void OnActivityResult(int requestCode, int resultCode, Intent intent)
        {
            base.OnActivityResult(requestCode, resultCode, intent);

            if(requestCode != RequestPicker || (Result) resultCode != Result.Ok)
                return;
            
            OnLocationSelected(intent.Data);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAutoBackupSetup, null);
            SetupToolbar(view, Resource.String.prefAutoBackupTitle);

            _hasPassword = SecureStorage.GetAsync("autoBackupPassword").GetAwaiter().GetResult() switch
            {
                null => null,
                "" => false,
                _ => true
            };
            
            var prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
            var backupEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            _backupLocationUri = prefs.GetString("pref_autoBackupUri", null);

            var selectLocationButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSelectLocation);
            selectLocationButton.Click += OnSelectLocationClick;

            var setPasswordButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSetPassword);
            setPasswordButton.Click += OnSetPasswordButtonClick;

            _locationStatusText = view.FindViewById<TextView>(Resource.Id.textLocationStatus);
            _passwordStatusText = view.FindViewById<TextView>(Resource.Id.textPasswordStatus);

            _testBackupButton = view.FindViewById<MaterialButton>(Resource.Id.buttonTestBackup);
            _testBackupButton.Click += OnTestBackupButtonClick;

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            _okButton.Click += OnOkButtonClick;

            _enabledSwitch = view.FindViewById<SwitchMaterial>(Resource.Id.switchEnabled);
            _enabledSwitch.Checked = backupEnabled;
            _enabledSwitch.CheckedChange += OnEnabledSwitchChanged;

            UpdateLocationStatusText();
            UpdatePasswordStatusText();
            UpdateEnabledSwitchAndTestButton();
            
            return view;
        }

        private void OnEnabledSwitchChanged(object sender, EventArgs e)
        {
            var editor = PreferenceManager.GetDefaultSharedPreferences(Context).Edit();
            editor.PutBoolean("pref_autoBackupEnabled", _enabledSwitch.Checked);
            editor.Commit();
        }

        private void OnTestBackupButtonClick(object sender, EventArgs e)
        {
            PreferenceManager.GetDefaultSharedPreferences(Context).Edit().PutBoolean("autoBackupTestRun", true).Commit();
            
            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            var manager = WorkManager.GetInstance(Context);
            manager.EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace, request);
            
            Toast.MakeText(Context, Resource.String.backupScheduled, ToastLength.Short).Show();
        }

        private void OnOkButtonClick(object sender, EventArgs e)
        {
            var workManager = WorkManager.GetInstance(Context);

            if(_enabledSwitch.Checked)
            {
                var workRequest = new PeriodicWorkRequest.Builder(typeof(AutoBackupWorker), 1, TimeUnit.Hours).Build();
                workManager.EnqueueUniquePeriodicWork(AutoBackupWorker.Name, ExistingPeriodicWorkPolicy.Keep, workRequest);
            }
            else
            {
                PreferenceManager.GetDefaultSharedPreferences(Context).Edit().PutBoolean("pref_autoBackupEnabled", false).Commit();
                workManager.CancelUniqueWork(AutoBackupWorker.Name);
            }

            Dismiss();
        }

        private void OnSelectLocationClick(object sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission);

            var autoBackupUri = PreferenceManager.GetDefaultSharedPreferences(Context).GetString("prefAutoBackupUri", null);
            if(autoBackupUri != null)
                intent.PutExtra(DocumentsContract.ExtraInitialUri, Uri.Parse(autoBackupUri));
            
            StartActivityForResult(intent, RequestPicker);
        }
        
        private void OnSetPasswordButtonClick(object sender, EventArgs e)
        {
            var fragment = new BackupPasswordBottomSheet(BackupPasswordBottomSheet.Mode.Set);
            fragment.PasswordEntered += OnPasswordEntered;
            
            var activity = (SettingsActivity) Context;
            fragment.Show(activity.SupportFragmentManager, fragment.Tag);
        }

        private async void OnPasswordEntered(object sender, string password)
        {
            _hasPassword = password != "";
            ((BackupPasswordBottomSheet) sender).Dismiss();
            UpdatePasswordStatusText();
            UpdateEnabledSwitchAndTestButton();
            await SecureStorage.SetAsync("autoBackupPassword", password);
        }

        private void OnLocationSelected(Uri uri)
        {
            _backupLocationUri = uri.ToString();
            Context.ContentResolver.TakePersistableUriPermission(uri, ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            
            var editor = PreferenceManager.GetDefaultSharedPreferences(Context).Edit();
            editor.PutString("pref_autoBackupUri", _backupLocationUri);
            editor.Commit();
            
            UpdateLocationStatusText();
            UpdateEnabledSwitchAndTestButton();
        }

        private void UpdateLocationStatusText()
        {
            _locationStatusText.Text = _backupLocationUri == null
                ? GetString(Resource.String.noLocationSelected)
                : String.Format(GetString(Resource.String.locationSetTo), _backupLocationUri);
        }

        private void UpdatePasswordStatusText()
        {
            _passwordStatusText.SetText(_hasPassword switch
            {
                null => Resource.String.passwordNotSet,
                false => Resource.String.notPasswordProtected,
                true => Resource.String.passwordSet
            });
        }

        private void UpdateEnabledSwitchAndTestButton()
        {
            if(_enabledSwitch.Checked)
                _enabledSwitch.Enabled = _testBackupButton.Enabled = true;
            else
            {
                _enabledSwitch.Enabled = _testBackupButton.Enabled = _backupLocationUri != null && _hasPassword != null;
                _enabledSwitch.Checked = false;
            }
        }
    }
}