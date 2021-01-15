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

        private SwitchMaterial _backupEnabledSwitch;
        private SwitchMaterial _restoreEnabledSwitch;
        private MaterialButton _triggerBackupButton;
        private MaterialButton _triggerRestoreButton;
        private MaterialButton _okButton;

        private bool _backupEnabled;
        private bool _restoreEnabled;
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
            
            OnLocationSelected(intent);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAutoBackupSetup, null);
            SetupToolbar(view, Resource.String.prefAutoBackupTitle, true);

            _hasPassword = SecureStorage.GetAsync("autoBackupPassword").GetAwaiter().GetResult() switch
            {
                null => null,
                "" => false,
                _ => true
            };
            
            var prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
            _backupEnabled = prefs.GetBoolean("pref_autoBackupEnabled", false);
            _restoreEnabled = prefs.GetBoolean("pref_autoRestoreEnabled", false);
            _backupLocationUri = prefs.GetString("pref_autoBackupUri", null);

            var selectLocationButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSelectLocation);
            selectLocationButton.Click += OnSelectLocationClick;

            var setPasswordButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSetPassword);
            setPasswordButton.Click += OnSetPasswordButtonClick;

            _locationStatusText = view.FindViewById<TextView>(Resource.Id.textLocationStatus);
            _passwordStatusText = view.FindViewById<TextView>(Resource.Id.textPasswordStatus);

            _triggerBackupButton = view.FindViewById<MaterialButton>(Resource.Id.buttonTriggerBackup);
            _triggerBackupButton.Click += OnTriggerBackupButtonClick;
            
            _triggerRestoreButton = view.FindViewById<MaterialButton>(Resource.Id.buttonTriggerRestore);
            _triggerRestoreButton.Click += OnTriggerRestoreButtonClick;

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            _okButton.Click += delegate { Dismiss(); };

            _backupEnabledSwitch = view.FindViewById<SwitchMaterial>(Resource.Id.switchBackupEnabled);
            _restoreEnabledSwitch = view.FindViewById<SwitchMaterial>(Resource.Id.switchRestoreEnabled);

            UpdateLocationStatusText();
            UpdatePasswordStatusText();
            UpdateSwitchesAndTriggerButton();
            
            return view;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            
            PreferenceManager.GetDefaultSharedPreferences(Context).Edit()
                .PutBoolean("pref_autoBackupEnabled", _backupEnabledSwitch.Checked)
                .PutBoolean("pref_autoRestoreEnabled", _restoreEnabledSwitch.Checked).Commit();
                
            var isEnabled = _backupEnabled || _restoreEnabled;
            var shouldBeEnabled = _backupEnabledSwitch.Checked || _restoreEnabledSwitch.Checked;
            
            if(isEnabled == shouldBeEnabled)
                return;
            
            var workManager = WorkManager.GetInstance(Context);

            if(!isEnabled)
            {
                var workRequest = new PeriodicWorkRequest.Builder(typeof(AutoBackupWorker), 30, TimeUnit.Minutes, 15, TimeUnit.Minutes).Build();
                workManager.EnqueueUniquePeriodicWork(AutoBackupWorker.Name, ExistingPeriodicWorkPolicy.Replace, workRequest);
            }
            else
                workManager.CancelUniqueWork(AutoBackupWorker.Name);
        }

        private void OnTriggerBackupButtonClick(object sender, EventArgs e)
        {
            PreferenceManager.GetDefaultSharedPreferences(Context).Edit().PutBoolean("autoBackupTrigger", true).Commit();
            TriggerWork();
            Toast.MakeText(Context, Resource.String.backupScheduled, ToastLength.Short).Show();
        }
        
        private void OnTriggerRestoreButtonClick(object sender, EventArgs e)
        {
            PreferenceManager.GetDefaultSharedPreferences(Context).Edit().PutBoolean("autoRestoreTrigger", true).Commit();
            TriggerWork();
            Toast.MakeText(Context, Resource.String.restoreScheduled, ToastLength.Short).Show();
        }

        private void TriggerWork()
        {
            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            var manager = WorkManager.GetInstance(Context);
            manager.EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace, request);
        }

        private void OnSelectLocationClick(object sender, EventArgs e)
        {
            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission | ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantPrefixUriPermission);

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
            UpdateSwitchesAndTriggerButton();
            await SecureStorage.SetAsync("autoBackupPassword", password);
        }

        private void OnLocationSelected(Intent intent)
        {
            _backupLocationUri = intent.Data.ToString();

            var flags = intent.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            Context.ContentResolver.TakePersistableUriPermission(intent.Data, flags);
            
            var editor = PreferenceManager.GetDefaultSharedPreferences(Context).Edit();
            editor.PutString("pref_autoBackupUri", _backupLocationUri);
            editor.Commit();
            
            UpdateLocationStatusText();
            UpdateSwitchesAndTriggerButton();
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

        private void UpdateSwitchesAndTriggerButton()
        {
            _backupEnabledSwitch.Checked = _backupEnabled;
            _restoreEnabledSwitch.Checked = _restoreEnabled;
           
            var canBeChecked = _backupLocationUri != null && _hasPassword != null;
            _backupEnabledSwitch.Enabled = _restoreEnabledSwitch.Enabled = canBeChecked;

            if(!canBeChecked)
                _backupEnabledSwitch.Checked = _restoreEnabledSwitch.Checked = false;
        }
    }
}