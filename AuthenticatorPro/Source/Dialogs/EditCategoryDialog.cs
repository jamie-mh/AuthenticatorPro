using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Google.Android.Material.TextField;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace AuthenticatorPro.Dialogs
{
    internal class EditCategoryDialog : DialogFragment
    {
        private readonly string _name;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly int _titleRes;

        private TextInputEditText _nameText;
        private TextInputLayout _nameTextLayout;

        public EditCategoryDialog(int titleRes, Action<object, EventArgs> positive, Action<object, EventArgs> negative,
            string name = null)
        {
            RetainInstance = true;

            _titleRes = titleRes;
            _name = name;
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public string Name => _nameText.Text;

        public string Error {
            set => _nameTextLayout.Error = value;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(_titleRes);

            alert.SetPositiveButton(_titleRes, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogEditCategory, null);
            _nameText = view.FindViewById<TextInputEditText>(Resource.Id.dialogEditCategory_name);
            _nameTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogEditCategory_nameLayout);

            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            if(_name != null) _nameText.Text = _name;

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }
    }
}