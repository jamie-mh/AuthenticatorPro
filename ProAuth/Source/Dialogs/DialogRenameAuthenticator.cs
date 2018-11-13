using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using ProAuth.Data;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace ProAuth.Dialogs
{
    internal class DialogRenameAuthenticator : DialogFragment
    {
        public string Issuer => _issuerText.Text;

        public string Username => _usernameText.Text;

        public string IssuerError 
        {
            set => _issuerText.Error = value;
        }

        public Authenticator Authenticator { get; }
        public int Position { get; }

        private EditText _issuerText;
        private EditText _usernameText;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        public DialogRenameAuthenticator(Action<object, EventArgs> positive, Action<object, EventArgs> negative, Authenticator auth, int position)
        {
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
            Authenticator = auth;
            Position = position;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.renameAuth);

            alert.SetPositiveButton(Resource.String.rename, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogRenameAuthenticator, null);
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogRenameAuthenticator_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogRenameAuthenticator_username);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();
            dialog.Show();

            _issuerText.Text = Authenticator.Issuer;
            _usernameText.Text = Authenticator.Username;

            Button renameButton = dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            renameButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }
    }
}