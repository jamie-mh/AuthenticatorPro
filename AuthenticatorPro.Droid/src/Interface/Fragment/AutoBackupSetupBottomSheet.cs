// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Core.Content;
using AndroidX.Work;
using AuthenticatorPro.Droid.Activity;
using AuthenticatorPro.Droid.Callback;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.SwitchMaterial;
using Java.Util.Concurrent;
using System;
using Uri = Android.Net.Uri;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class AutoBackupSetupBottomSheet : BottomSheet
    {
        private PreferenceWrapper _preferences;
        private ActivityResultLauncher _locationSelectResultLauncher;
        private ActivityResultLauncher _showNotificationsResultLauncher;

        private TextView _locationStatusText;
        private TextView _passwordStatusText;

        private SwitchMaterial _backupEnabledSwitch;
        private MaterialButton _backupNowButton;
        private MaterialButton _okButton;

        public AutoBackupSetupBottomSheet() : base(Resource.Layout.sheetAutoBackupSetup) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var locationSelectCallback = new ActivityResultCallback();
            locationSelectCallback.Result += OnLocationSelectResult;

            _locationSelectResultLauncher =
                RegisterForActivityResult(new ActivityResultContracts.StartActivityForResult(), locationSelectCallback);

            var showNotificationsPermissionCallback = new ActivityResultCallback();
            showNotificationsPermissionCallback.Result += OnShowNotificationsPermissionResult;

            _showNotificationsResultLauncher =
                RegisterForActivityResult(new ActivityResultContracts.RequestPermission(), showNotificationsPermissionCallback);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.prefAutoBackupTitle, true);

            _preferences = new PreferenceWrapper(Context);

            var selectLocationButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSelectLocation);
            selectLocationButton.Click += OnSelectLocationClick;

            var setPasswordButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSetPassword);
            setPasswordButton.Click += OnSetPasswordButtonClick;

            _locationStatusText = view.FindViewById<TextView>(Resource.Id.textLocationStatus);
            _passwordStatusText = view.FindViewById<TextView>(Resource.Id.textPasswordStatus);

            _backupNowButton = view.FindViewById<MaterialButton>(Resource.Id.buttonBackupNow);
            _backupNowButton.Click += OnBackupNowButtonClick;

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOk);
            _okButton.Click += delegate { Dismiss(); };

            _backupEnabledSwitch = view.FindViewById<SwitchMaterial>(Resource.Id.switchBackupEnabled);
            _backupEnabledSwitch.Click += OnSwitchClicked;

            UpdateLocationStatusText();
            UpdateSwitchAndTriggerButton();

            if (_preferences.DatabasePasswordBackup)
            {
                setPasswordButton.Visibility = ViewStates.Gone;
            }
            else
            {
                UpdatePasswordStatusText();
            }

            return view;
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);

            _preferences.AutoBackupEnabled = _backupEnabledSwitch.Checked;
            var workManager = WorkManager.GetInstance(RequireContext());

            if (_backupEnabledSwitch.Checked)
            {
                var workRequest =
                    new PeriodicWorkRequest.Builder(typeof(AutoBackupWorker), 15, TimeUnit.Minutes!).Build();
                workManager.EnqueueUniquePeriodicWork(AutoBackupWorker.Name, ExistingPeriodicWorkPolicy.Keep!,
                    workRequest);
            }
            else
            {
                workManager.CancelUniqueWork(AutoBackupWorker.Name);
            }
        }

        private void OnLocationSelectResult(object sender, Object obj)
        {
            var result = (ActivityResult) obj;
            var intent = result.Data;

            if ((Result) result.ResultCode != Result.Ok || intent.Data == null)
            {
                return;
            }

            _preferences.AutoBackupUri = intent.Data;

            var flags = intent.Flags &
                        (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            Context.ContentResolver.TakePersistableUriPermission(intent.Data, flags);

            UpdateLocationStatusText();
            UpdateSwitchAndTriggerButton();
        }

        private void OnShowNotificationsPermissionResult(object sender, Object obj)
        {
            ShowBatteryOptimisationDialog();
        }

        private void OnSwitchClicked(object sender, EventArgs e)
        {
            if (!_backupEnabledSwitch.Checked)
            {
                return;
            }

            if (ContextCompat.CheckSelfPermission(RequireContext(), Manifest.Permission.PostNotifications) != Permission.Granted)
            {
                _showNotificationsResultLauncher.Launch(Manifest.Permission.PostNotifications);
            }
            else
            {
                ShowBatteryOptimisationDialog();
            }
        }

        private void OnBackupNowButtonClick(object sender, EventArgs e)
        {
            _preferences.AutoBackupTrigger = true;
            TriggerWork();
            Toast.MakeText(Context, Resource.String.backupScheduled, ToastLength.Short).Show();
        }

        private void TriggerWork()
        {
            var request = new OneTimeWorkRequest.Builder(typeof(AutoBackupWorker)).Build();
            var manager = WorkManager.GetInstance(RequireContext());
            manager.EnqueueUniqueWork(AutoBackupWorker.Name, ExistingWorkPolicy.Replace!, request);
        }

        private void OnSelectLocationClick(object sender, EventArgs args)
        {
            var intent = new Intent(Intent.ActionOpenDocumentTree);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission |
                            ActivityFlags.GrantPersistableUriPermission | ActivityFlags.GrantPrefixUriPermission);

            if (_preferences.AutoBackupUri != null)
            {
                intent.PutExtra(DocumentsContract.ExtraInitialUri, _preferences.AutoBackupUri);
            }

            var baseApplication = ((SettingsActivity) Context).BaseApplication;
            baseApplication.PreventNextAutoLock = true;

            try
            {
                _locationSelectResultLauncher.Launch(intent);
            }
            catch (ActivityNotFoundException e)
            {
                Logger.Error(e);
                Toast.MakeText(Context, Resource.String.filePickerMissing, ToastLength.Long);
                baseApplication.PreventNextAutoLock = false;
            }
        }

        private void OnSetPasswordButtonClick(object sender, EventArgs e)
        {
            var bundle = new Bundle();
            bundle.PutInt("mode", (int) BackupPasswordBottomSheet.Mode.Set);

            var fragment = new BackupPasswordBottomSheet { Arguments = bundle };
            fragment.PasswordEntered += OnPasswordEntered;

            var activity = (SettingsActivity) Context;
            fragment.Show(activity.SupportFragmentManager, fragment.Tag);
        }

        private async void OnPasswordEntered(object sender, string password)
        {
            _preferences.AutoBackupPasswordProtected = password != "";
            ((BackupPasswordBottomSheet) sender).Dismiss();
            UpdatePasswordStatusText();
            UpdateSwitchAndTriggerButton();
            await SecureStorageWrapper.SetAutoBackupPassword(password);
        }

        private void UpdateLocationStatusText()
        {
            var uri = _preferences.AutoBackupUri;

            if (uri == null)
            {
                _locationStatusText.SetText(Resource.String.noLocationSelected);
                return;
            }

            string dirName;

            try
            {
                dirName = FileUtil.GetDocumentName(Context.ContentResolver, uri);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                dirName = "Unknown";
            }

            _locationStatusText.Text = String.Format(GetString(Resource.String.locationSetTo), dirName);
        }

        private void UpdatePasswordStatusText()
        {
            _passwordStatusText.SetText(_preferences.AutoBackupPasswordProtected switch
            {
                null => Resource.String.passwordNotSet,
                false => Resource.String.notPasswordProtected,
                true => Resource.String.passwordSet
            });
        }

        private void UpdateSwitchAndTriggerButton()
        {
            _backupEnabledSwitch.Checked = _preferences.AutoBackupEnabled;

            var canBeChecked = _preferences.AutoBackupUri != null &&
                (_preferences.DatabasePasswordBackup || _preferences.AutoBackupPasswordProtected != null);

            _backupEnabledSwitch.Enabled = _backupNowButton.Enabled = canBeChecked;

            if (!canBeChecked)
            {
                _backupEnabledSwitch.Checked = false;
            }
        }

        private void ShowBatteryOptimisationDialog()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
            {
                return;
            }

            var powerManager = (PowerManager) Context.GetSystemService(Context.PowerService);

            if (powerManager.IsIgnoringBatteryOptimizations(Context.PackageName))
            {
                return;
            }

            var builder = new MaterialAlertDialogBuilder(Context)
                .SetTitle(Resource.String.batOptim)
                .SetMessage(Resource.String.disableBatOptimMessage)
                .SetNegativeButton(Resource.String.ignore, delegate { })
                .SetPositiveButton(Resource.String.disable, delegate
                {
                    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(Uri.Parse($"package:{Context.PackageName}"));
                    StartActivity(intent);
                });

            builder.Create().Show();
        }
    }
}