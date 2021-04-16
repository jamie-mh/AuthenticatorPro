using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class BackupPasswordBottomSheet : BottomSheet
    {
        public enum Mode
        {
            Set = 0,
            Enter = 1
        }

        private Mode _mode;

        public event EventHandler Cancel;
        public event EventHandler<string> PasswordEntered;

        private ProgressBar _progressBar;
        private TextInputEditText _passwordText;
        private TextInputLayout _passwordTextLayout;
        
        private MaterialButton _cancelButton;
        private MaterialButton _okButton;

        public string Error
        {
            set => _passwordTextLayout.Error = value;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _mode = (Mode) Arguments.GetInt("mode", 0);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetBackupPassword, null);
            SetupToolbar(view, Resource.String.password);

            _progressBar = view.FindViewById<ProgressBar>(Resource.Id.appBarProgressBar);
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _passwordTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            _okButton.Click += OnOkButtonClick;

            _cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            _cancelButton.Click += OnCancelButtonClick;

            if(_mode == Mode.Set)
            {
                var message = view.FindViewById<TextView>(Resource.Id.textMessage);
                message.Visibility = ViewStates.Visible; 
            }

            _passwordText.EditorAction += (_, args) =>
            {
                if(args.ActionId == ImeAction.Done)
                    _okButton.PerformClick();
            };
            
            TextInputUtil.EnableAutoErrorClear(_passwordTextLayout);

            return view;
        }

        private void OnOkButtonClick(object s, EventArgs e)
        {
            if(_mode == Mode.Set && _passwordText.Text == "")
            {
                var builder = new MaterialAlertDialogBuilder(Activity);
                builder.SetTitle(Resource.String.warning);
                builder.SetMessage(Resource.String.confirmEmptyPassword);
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

        public void SetBusyText(int? busyRes)
        {
            var busy = busyRes != null;
            SetCancelable(!busy);
            _progressBar.Visibility = busy ? ViewStates.Visible : ViewStates.Invisible;
            _okButton.Enabled = _cancelButton.Enabled = !busy;
            _okButton.SetText(busyRes ?? Resource.String.ok);
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            Dismiss();
            Cancel?.Invoke(this, null);
        }
    }
}