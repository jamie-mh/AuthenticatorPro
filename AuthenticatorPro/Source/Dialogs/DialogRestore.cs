using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace AuthenticatorPro.Dialogs
{
    internal class DialogRestore : DialogFragment
    {
        private readonly Action<object, EventArgs> _negativeButtonEvent;
        private readonly Action<object, EventArgs> _positiveButtonEvent;

        private EditText _passwordText;

        public DialogRestore(Action<object, EventArgs> positive, Action<object, EventArgs> negative)
        {
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public string Password => _passwordText.Text;

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.restore);

            alert.SetPositiveButton(Resource.String.restore, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogRestore, null);
            _passwordText = view.FindViewById<EditText>(Resource.Id.dialogRestore_password);
            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();

            var restoreButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            restoreButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

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