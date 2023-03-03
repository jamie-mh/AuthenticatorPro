// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Core.Entity;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using System;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    internal class RenameAuthenticatorBottomSheet : BottomSheet
    {
        public event EventHandler<RenameEventArgs> RenameClicked;

        private string _secret;
        private string _issuer;
        private string _username;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;

        public RenameAuthenticatorBottomSheet() : base(Resource.Layout.sheetRenameAuthenticator) { }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _secret = Arguments.GetString("secret");
            _issuer = Arguments.GetString("issuer");
            _username = Arguments.GetString("username");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.rename);

            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.editIssuerLayout);
            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.editIssuer);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editUsernameLayout);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);

            _issuerText.Append(_issuer);

            if (_username != null)
            {
                _usernameText.Append(_username);
            }

            _issuerLayout.CounterMaxLength = Authenticator.IssuerMaxLength;
            _issuerText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(Authenticator.IssuerMaxLength) });
            _usernameLayout.CounterMaxLength = Authenticator.UsernameMaxLength;
            _usernameText.SetFilters(
                new IInputFilter[] { new InputFilterLengthFilter(Authenticator.UsernameMaxLength) });

            TextInputUtil.EnableAutoErrorClear(_issuerLayout);

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate
            {
                Dismiss();
            };

            var renameButton = view.FindViewById<MaterialButton>(Resource.Id.buttonRename);
            renameButton.Click += delegate
            {
                var issuer = _issuerText.Text.Trim();
                if (issuer == "")
                {
                    _issuerLayout.Error = GetString(Resource.String.noIssuer);
                    return;
                }

                var args = new RenameEventArgs(_secret, issuer, _usernameText.Text);
                RenameClicked?.Invoke(this, args);
                Dismiss();
            };

            _usernameText.EditorAction += (_, args) =>
            {
                if (args.ActionId == ImeAction.Done)
                {
                    renameButton.PerformClick();
                }
            };

            return view;
        }

        public class RenameEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly string Issuer;
            public readonly string Username;

            public RenameEventArgs(string secret, string issuer, string username)
            {
                Secret = secret;
                Issuer = issuer;
                Username = username;
            }
        }
    }
}