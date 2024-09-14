// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Stratum.Droid.Util;

namespace Stratum.Droid.Interface.Fragment
{
    public class PinBottomSheet : BottomSheet
    {
        private TextInputEditText _pinText;
        private TextInputLayout _pinTextLayout;

        private MaterialButton _cancelButton;
        private MaterialButton _okButton;

        private int _length;

        public PinBottomSheet() : base(Resource.Layout.sheetPin, Resource.String.pin)
        {
        }

        public event EventHandler CancelClicked;
        public event EventHandler<string> PinEntered;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _length = Arguments.GetInt("length", 0);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            _pinText = view.FindViewById<TextInputEditText>(Resource.Id.editPin);
            _pinTextLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPinLayout);

            _pinTextLayout.CounterMaxLength = _length;
            _pinText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(_length) });

            _okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            _okButton.Click += OnOkButtonClick;

            _cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            _cancelButton.Click += OnCancelButtonClick;

            _pinText.EditorAction += (_, args) =>
            {
                if (args.ActionId == ImeAction.Done)
                {
                    _okButton.PerformClick();
                }
            };

            TextInputUtil.EnableAutoErrorClear(_pinTextLayout);

            return view;
        }

        private void OnOkButtonClick(object s, EventArgs e)
        {
            if (_pinText.Text.Length < _length)
            {
                var error = string.Format(GetString(Resource.String.pinTooShort), _length);
                _pinTextLayout.Error = error;
                return;
            }

            PinEntered?.Invoke(this, _pinText.Text);
        }

        private void OnCancelButtonClick(object sender, EventArgs e)
        {
            Dismiss();
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}