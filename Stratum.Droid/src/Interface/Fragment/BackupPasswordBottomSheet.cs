// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextField;
using Google.Android.Material.TextView;
using Stratum.Droid.Util;

namespace Stratum.Droid.Interface.Fragment
{
    public class BackupPasswordBottomSheet : BottomSheet
    {
        public enum Mode
        {
            Set = 0,
            Enter = 1
        }

        private Mode _mode;

        private TextInputEditText _passwordText;
        private TextInputLayout _passwordTextLayout;

        private MaterialButton _cancelButton;
        private MaterialButton _okButton;
        private CircularProgressIndicator _progressIndicator;

        public BackupPasswordBottomSheet() : base(Resource.Layout.sheetBackupPassword, Resource.String.password)
        {
        }

        public string Error
        {
            set => _passwordTextLayout.Error = value;
        }

        public event EventHandler CancelClicked;
        public event EventHandler<string> PasswordEntered;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _mode = (Mode) Arguments.GetInt("mode", 0);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _passwordTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);

            _progressIndicator = view.FindViewById<CircularProgressIndicator>(Resource.Id.progressIndicator);

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            _okButton.Click += OnOkButtonClick;

            _cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            _cancelButton.Click += OnCancelButtonClick;

            if (_mode == Mode.Set)
            {
                var message = view.FindViewById<MaterialTextView>(Resource.Id.textMessage);
                message.Visibility = ViewStates.Visible;
            }

            _passwordText.EditorAction += (_, args) =>
            {
                if (args.ActionId == ImeAction.Done)
                {
                    _okButton.PerformClick();
                }
            };

            TextInputUtil.EnableAutoErrorClear(_passwordTextLayout);

            return view;
        }

        private void OnOkButtonClick(object s, EventArgs e)
        {
            if (_mode == Mode.Set && _passwordText.Text == "")
            {
                var builder = new MaterialAlertDialogBuilder(RequireContext());
                builder.SetTitle(Resource.String.warning);
                builder.SetMessage(Resource.String.confirmEmptyPassword);
                builder.SetIcon(Resource.Drawable.baseline_warning_24);
                builder.SetCancelable(true);

                builder.SetNegativeButton(Resource.String.cancel, delegate { });
                builder.SetPositiveButton(Resource.String.ok, (sender, _) =>
                {
                    ((AlertDialog) sender).Dismiss();
                    PasswordEntered?.Invoke(this, "");
                });

                builder.Create().Show();
                return;
            }

            PasswordEntered?.Invoke(this, _passwordText.Text);
        }

        public void SetLoading(bool loading)
        {
            SetCancelable(!loading);

            if (loading)
            {
                _okButton.Visibility = ViewStates.Invisible;
                _cancelButton.Enabled = false;
                _progressIndicator.Visibility = ViewStates.Visible;
            }
            else
            {
                _okButton.Visibility = ViewStates.Visible;
                _cancelButton.Enabled = true;
                _progressIndicator.Visibility = ViewStates.Invisible;
            }
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            Dismiss();
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}