// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Entity;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using System;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class EditCategoryBottomSheet : BottomSheet
    {
        public enum Mode
        {
            New, Edit
        }

        public event EventHandler<EditCategoryEventArgs> Submitted;

        private Mode _mode;
        private int _position;
        private string _initialValue;

        private TextInputEditText _textName;
        private TextInputLayout _textNameLayout;

        public string NameError
        {
            set => _textNameLayout.Error = value;
        }

        public EditCategoryBottomSheet() : base(Resource.Layout.sheetEditCategory) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _mode = (Mode) Arguments.GetInt("mode", 0);
            _position = Arguments.GetInt("position", -1);
            _initialValue = Arguments.GetString("initialValue");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var titleRes = _mode switch
            {
                Mode.Edit => Resource.String.rename,
                _ => Resource.String.add
            };

            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, titleRes);

            _textName = view.FindViewById<TextInputEditText>(Resource.Id.editName);
            _textNameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editNameLayout);
            _textNameLayout.CounterMaxLength = Category.NameMaxLength;
            _textName.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(Category.NameMaxLength) });

            var submitButton = view.FindViewById<MaterialButton>(Resource.Id.buttonSubmit);
            submitButton.SetText(titleRes);

            if (_initialValue != null)
            {
                _textName.Append(_initialValue);
            }

            _textName.EditorAction += (_, args) =>
            {
                if (args.ActionId == ImeAction.Done)
                {
                    submitButton.PerformClick();
                }
            };

            TextInputUtil.EnableAutoErrorClear(_textNameLayout);

            submitButton.Click += delegate
            {
                var name = _textName.Text.Trim();

                if (name == "")
                {
                    _textNameLayout.Error = GetString(Resource.String.noCategoryName);
                    return;
                }

                var args = new EditCategoryEventArgs(_position, _initialValue, name);
                Submitted?.Invoke(this, args);
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
            public readonly int Position;
            public readonly string InitialName;
            public readonly string Name;

            public EditCategoryEventArgs(int position, string initialName, string name)
            {
                Position = position;
                InitialName = initialName;
                Name = name;
            }
        }
    }
}