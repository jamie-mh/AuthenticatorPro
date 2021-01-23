using System;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;

namespace AuthenticatorPro.Droid.Fragment
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
            var titleRes = _mode switch
            {
                Mode.Edit => Resource.String.rename,
                _ => Resource.String.add
            };

            var view = inflater.Inflate(Resource.Layout.sheetEditCategory, null);
            SetupToolbar(view, titleRes);
            
            _textName = view.FindViewById<TextInputEditText>(Resource.Id.editName);
            _textNameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editNameLayout);
            _textNameLayout.CounterMaxLength = Category.NameMaxLength;
            _textName.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(Category.NameMaxLength) });

            var submitButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSubmit);
            submitButton.SetText(titleRes);

            if(_initialValue != null)
                _textName.Append(_initialValue);

            _textName.EditorAction += (_, args) =>
            {
                if(args.ActionId == ImeAction.Done)
                    submitButton.PerformClick();
            };
            
            TextInputUtil.EnableAutoErrorClear(_textNameLayout);

            submitButton.Click += delegate
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