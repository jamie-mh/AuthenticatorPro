using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;


namespace AuthenticatorPro.Fragment
{
    internal class EditCategoryBottomSheet : BottomSheetDialogFragment
    {
        public enum Mode
        {
            New, Edit
        }

        public event EventHandler<EditCategoryEventArgs> Submit;

        private readonly Mode _mode;
        private readonly string _initialValue;

        private TextInputEditText _textName;
        private TextInputLayout _textNameLayout;

        public string NameError {
            set => _textNameLayout.Error = value;
        }


        public EditCategoryBottomSheet(Mode mode, string initialValue = null)
        {
            RetainInstance = true;
            _mode = mode;
            _initialValue = initialValue;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            int titleRes;

            switch(_mode)
            {
                default: titleRes = Resource.String.add; break;
                case Mode.Edit: titleRes = Resource.String.rename; break;
            }

            var view = inflater.Inflate(Resource.Layout.sheetEditCategory, null);
            _textName = view.FindViewById<TextInputEditText>(Resource.Id.editName);
            _textNameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editNameLayout);

            var title = view.FindViewById<TextView>(Resource.Id.textTitle);
            title.SetText(titleRes);

            var submitButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSubmit);
            submitButton.SetText(titleRes);

            if(_initialValue != null)
                _textName.Text = _initialValue;

            submitButton.Click += (sender, e) =>
            {
                var name = _textName.Text.Trim();

                if(name == "")
                {
                    _textNameLayout.Error = GetString(Resource.String.noCategoryName);
                    return;
                }

                var args = new EditCategoryEventArgs(_initialValue, name);
                Submit?.Invoke(this, args);
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += (sender, e) =>
            {
                Dismiss();
            };

            _textName.EditorAction += (sender, args) =>
            {
                if(args.ActionId == ImeAction.Done)
                    submitButton.PerformClick();
            };

            return view;
        }

        public class EditCategoryEventArgs : EventArgs
        {
            public readonly string InitialName;
            public readonly string Name;

            public EditCategoryEventArgs(string initialName, string name)
            {
                InitialName = initialName;
                Name = name;
            }
        }
    }
}