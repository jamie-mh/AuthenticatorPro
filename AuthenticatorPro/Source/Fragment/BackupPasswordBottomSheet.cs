using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Fragment
{
    internal class BackupPasswordBottomSheet : ExpandedBottomSheetDialogFragment
    {
        public enum Mode
        {
            Backup = 0,
            Restore = 1
        }

        private readonly Mode _mode;

        public event EventHandler Cancel;
        public event EventHandler<string> PasswordEntered;

        private TextInputEditText _passwordText;
        private TextInputLayout _passwordTextLayout;

        public BackupPasswordBottomSheet(Mode mode)
        {
            RetainInstance = true;
            _mode = mode;
        }

        public string Error
        {
            set => _passwordTextLayout.Error = value;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetBackupPassword, null);
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.editPassword);
            _passwordTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPasswordLayout);


            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            okButton.Click += OnOkButtonClick;

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += OnCancelButtonClick;

            if(_mode == Mode.Backup)
            {
                var message = view.FindViewById<TextView>(Resource.Id.textMessage);
                message.Visibility = ViewStates.Visible; 
            }

            _passwordText.EditorAction += (sender, args) =>
            {
                if(args.ActionId == ImeAction.Done)
                    okButton.PerformClick();
            };

            return view;
        }

        private void OnOkButtonClick(object s, EventArgs e)
        {
            if(_mode == Mode.Backup && _passwordText.Text == "")
            {
                var builder = new MaterialAlertDialogBuilder(Activity);
                builder.SetTitle(Resource.String.warning);
                builder.SetMessage(Resource.String.confirmEmptyPassword);
                builder.SetNegativeButton(Resource.String.no, (s, args) => { });

                builder.SetPositiveButton(Resource.String.yes, async (sender, args) =>
                {
                    ((AlertDialog) sender).Dismiss();
                    PasswordEntered?.Invoke(this, null);
                });

                builder.SetCancelable(true);
                builder.Create().Show();

                return;
            }

            PasswordEntered?.Invoke(this, _passwordText.Text);
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            Cancel?.Invoke(this, null);
        }
    }
}