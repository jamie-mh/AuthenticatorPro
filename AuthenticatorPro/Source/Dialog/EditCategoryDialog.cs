using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Google.Android.Material.TextField;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;


namespace AuthenticatorPro.Dialog
{
    internal class EditCategoryDialog : DialogFragment
    {
        public enum Mode
        {
            New, Edit
        }

        public event EventHandler Submit;

        private readonly Mode _mode;
        private readonly string _initialValue;

        private TextInputEditText _nameText;
        private TextInputLayout _nameTextLayout;


        public EditCategoryDialog(Mode mode, string initialValue = null)
        {
            RetainInstance = true;
            _mode = mode;
            _initialValue = initialValue;
        }

        public string Name => _nameText.Text;

        public string Error {
            set => _nameTextLayout.Error = value;
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            int titleRes;

            switch(_mode)
            {
                default: titleRes = Resource.String.add; break;
                case Mode.Edit: titleRes = Resource.String.rename; break;
            }

            alert.SetTitle(titleRes);
            alert.SetPositiveButton(titleRes, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogEditCategory, null);
            _nameText = view.FindViewById<TextInputEditText>(Resource.Id.dialogEditCategory_name);
            _nameTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogEditCategory_nameLayout);

            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            if(_initialValue != null)
                _nameText.Text = _initialValue;

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var negativeButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += Submit;
            negativeButton.Click += (sender, e) =>
            {
                dialog.Dismiss();
            };

            return dialog;
        }
    }
}