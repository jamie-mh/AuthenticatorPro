using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.TextField;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;
using FragmentTransaction = AndroidX.Fragment.App.FragmentTransaction;

namespace AuthenticatorPro.Dialogs
{
    internal class BackupPasswordDialog : DialogFragment
    {
        public enum Mode
        {
            Backup = 0,
            Restore = 1
        }

        private readonly Mode _mode;
        private readonly Action _negativeButtonEvent;
        private readonly Action _positiveButtonEvent;

        private TextInputEditText _passwordText;
        private TextInputLayout _passwordTextLayout;

        public BackupPasswordDialog(Mode mode, Action positive, Action negative)
        {
            RetainInstance = true;

            _mode = mode;
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public string Password => _passwordText.Text;
        public string Error
        {
            set => _passwordTextLayout.Error = value;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.password);

            alert.SetPositiveButton(Resource.String.ok, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogBackupPassword, null);
            _passwordText = view.FindViewById<TextInputEditText>(Resource.Id.dialogBackupPassword_password);
            _passwordTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogBackupPassword_passwordLayout);

            if(_mode == Mode.Backup)
            {
                var message = view.FindViewById<TextView>(Resource.Id.dialogBackupPassword_backupMessage);
                message.Visibility = ViewStates.Visible; 
            }

            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            var positiveButton = dialog.GetButton((int) DialogButtonType.Positive);
            var negativeButton = dialog.GetButton((int) DialogButtonType.Negative);

            positiveButton.Click += (sender, args) =>
            {
                _positiveButtonEvent?.Invoke();
            };

            negativeButton.Click += (sender, args) =>
            {
                _negativeButtonEvent?.Invoke();
                Dismiss();
            };

            return dialog;
        }

        public override int Show(FragmentTransaction transaction, string tag)
        {
            transaction.Add(this, tag).AddToBackStack(null);
            transaction.CommitAllowingStateLoss();

            return 1;
        }
    }
}