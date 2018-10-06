using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using OtpSharp;
using ProAuth.Data;
using ProAuth.Utilities;

namespace ProAuth
{
    class RenameDialog : DialogFragment
    {
        private AlertDialog _dialog;
        private Database _database;
        private AuthSource _authSource;
        private Authenticator _authenticator;

        private EditText _issuerText;
        private EditText _usernameText;

        public RenameDialog(Database database, AuthSource authSource, int authNo)
        {
            _database = database;
            _authSource = authSource;

            _authenticator = _authSource.GetNth(authNo);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        private void FindViews(View view)
        {
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogRename_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogRename_username);
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.renameAuth);

            alert.SetPositiveButton(Resource.String.rename, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogRename, null);
            FindViews(view);
            alert.SetView(view);

            _dialog = alert.Create();
            _dialog.Show();

            _issuerText.Text = _authenticator.Issuer;
            _usernameText.Text = _authenticator.Username;

            // Button listeners
            Button renameButton = _dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = _dialog.GetButton((int) DialogButtonType.Negative);

            renameButton.Click += RenameClick;
            cancelButton.Click += CancelClick;

            return _dialog;
        }

        private void RenameClick(object sender, EventArgs e)
        {
            if(_issuerText.Text.Trim() == "")
            {
                Toast.MakeText(_dialog.Context, Resource.String.noIssuer, ToastLength.Short).Show();
                return;
            }

            string issuer = StringExt.Truncate(_issuerText.Text.Trim(), 32);
            string username = StringExt.Truncate(_usernameText.Text.Trim(), 32);

            _authenticator.Issuer = issuer;
            _authenticator.Username = username;

            _database.Connection.Update(_authenticator);
            _authSource.ClearCache();
            _dialog?.Dismiss();
        }

        private void CancelClick(object sender, EventArgs e)
        {
            _dialog?.Dismiss();
        }
    }
}