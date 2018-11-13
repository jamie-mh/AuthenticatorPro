using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using DialogFragment = Android.Support.V4.App.DialogFragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace ProAuth.Dialogs
{
    internal class DialogRestore : DialogFragment
    {
        public string Password => _passwordText.Text;

        private EditText _passwordText;
        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        public DialogRestore(Action<object, EventArgs> positive, Action<object, EventArgs> negative)
        {
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.restore);

            alert.SetPositiveButton(Resource.String.restore, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogRestore, null);
            _passwordText = view.FindViewById<EditText>(Resource.Id.dialogRestore_password);
            alert.SetView(view);

            AlertDialog dialog = alert.Create();
            dialog.Show();

            Button restoreButton = dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

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