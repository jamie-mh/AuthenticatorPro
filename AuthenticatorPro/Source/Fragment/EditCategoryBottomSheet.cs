using System;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using AuthenticatorPro.Data;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;


namespace AuthenticatorPro.Fragment
{
    internal class EditCategoryBottomSheet : BottomSheet
    {
        public enum Mode
        {
            New, Edit
        }

        public event EventHandler<EditCategoryEventArgs> Submit;

        private readonly Mode _mode;
        private readonly int? _itemPosition;
        private readonly string _initialValue;

        private TextInputEditText _textName;
        private TextInputLayout _textNameLayout;

        public string NameError {
            set => _textNameLayout.Error = value;
        }


        public EditCategoryBottomSheet(Mode mode, int? itemPosition, string initialValue = null)
        {
            RetainInstance = true;
            _mode = mode;
            _itemPosition = itemPosition;
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
            SetupToolbar(view, titleRes);
            
            _textName = view.FindViewById<TextInputEditText>(Resource.Id.editName);
            _textNameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editNameLayout);
            _textNameLayout.CounterMaxLength = Category.NameMaxLength;

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

                var args = new EditCategoryEventArgs(_itemPosition, _initialValue, name);
                Submit?.Invoke(this, args);
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate
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
            public readonly int? ItemPosition;
            public readonly string InitialName;
            public readonly string Name;

            public EditCategoryEventArgs(int? itemPosition, string initialName, string name)
            {
                ItemPosition = itemPosition;
                InitialName = initialName;
                Name = name;
            }
        }
    }
}