// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Stratum.Core.Entity;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Stratum.Droid.Util;

namespace Stratum.Droid.Interface.Fragment
{
    public class EditCategoryBottomSheet : BottomSheet
    {
        public enum Mode
        {
            New,
            Edit
        }

        private Mode _mode;
        private string _id;
        private string _initialValue;

        private TextInputEditText _textName;
        private TextInputLayout _textNameLayout;

        public EditCategoryBottomSheet() : base(Resource.Layout.sheetEditCategory, Resource.String.category)
        {
        }

        public string NameError
        {
            set => _textNameLayout.Error = value;
        }

        public event EventHandler<EditCategoryEventArgs> Submitted;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _mode = (Mode) Arguments.GetInt("mode", 0);
            _id = Arguments.GetString("id");
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

                var args = new EditCategoryEventArgs(_id, _initialValue, name);
                Submitted?.Invoke(this, args);
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate { Dismiss(); };

            return view;
        }

        public class EditCategoryEventArgs : EventArgs
        {
            public readonly string Id;
            public readonly string InitialName;
            public readonly string Name;

            public EditCategoryEventArgs(string id, string initialName, string name)
            {
                Id = id;
                InitialName = initialName;
                Name = name;
            }
        }
    }
}