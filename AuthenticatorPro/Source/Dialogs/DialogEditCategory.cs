using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace AuthenticatorPro.Dialogs
{
    internal class DialogEditCategory : DialogFragment
    {
        private readonly string _name;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly int _titleRes;

        private EditText _nameText;

        public DialogEditCategory(int titleRes, Action<object, EventArgs> positive, Action<object, EventArgs> negative,
            string name = null)
        {
            _titleRes = titleRes;
            _name = name;
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public string Name => _nameText.Text;

        public string NameError {
            set => _nameText.Error = value;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(_titleRes);

            alert.SetPositiveButton(_titleRes, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogEditCategory, null);
            _nameText = view.FindViewById<EditText>(Resource.Id.dialogEditCategory_name);
            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();

            if(_name != null) _nameText.Text = _name;

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }
    }
}