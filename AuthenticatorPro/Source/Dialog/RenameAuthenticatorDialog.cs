using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.TextField;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace AuthenticatorPro.Dialog
{
    internal class RenameAuthenticatorDialog : DialogFragment
    {
        public event EventHandler<RenameEventArgs> Rename;

        private TextInputLayout _issuerTextLayout;
        private EditText _issuerText;
        private EditText _usernameText;

        private readonly int _itemPosition;
        private readonly string _issuer;
        private readonly string _username;

        public string IssuerError {
            set => _issuerTextLayout.Error = value;
        }

        public RenameAuthenticatorDialog(string issuer, string username, int itemPosition)
        {
            RetainInstance = true;

            _issuer = issuer;
            _username = username;
            _itemPosition = itemPosition;
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.rename);

            alert.SetPositiveButton(Resource.String.rename, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogRenameAuthenticator, null);
            _issuerTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogRenameAuthenticator_issuerLayout);
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogRenameAuthenticator_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogRenameAuthenticator_username);
            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            _issuerText.Text = _issuer;
            _usernameText.Text = _username;

            var renameButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            renameButton.Click += (sender, e) =>
            {
                var args = new RenameEventArgs(_itemPosition, _issuerText.Text, _usernameText.Text);
                Rename?.Invoke(this, args);
            };

            cancelButton.Click += (sender, e) =>
            {
                dialog.Dismiss();
            };

            return dialog;
        }

        public class RenameEventArgs : EventArgs
        {
            public readonly int ItemPosition;
            public readonly string Issuer;
            public readonly string Username;

            public RenameEventArgs(int itemPosition, string issuer, string username)
            {
                ItemPosition = itemPosition;
                Issuer = issuer;
                Username = username;
            }
        }
    }
}