using System;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using AuthenticatorPro.Droid.Data;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class RenameAuthenticatorBottomSheet : BottomSheet
    {
        public event EventHandler<RenameEventArgs> Rename;

        private readonly int _itemPosition;
        private readonly string _issuer;
        private readonly string _username;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;


        public RenameAuthenticatorBottomSheet(int itemPosition, string issuer, string username)
        {
            RetainInstance = true;
            _itemPosition = itemPosition;
            _issuer = issuer;
            _username = username;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetRenameAuthenticator, null);
            SetupToolbar(view, Resource.String.rename);
            
            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.editIssuerLayout);
            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.editIssuer);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editUsernameLayout);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);

            _issuerText.Append(_issuer);
            
            if(_username != null)
                _usernameText.Append(_username);

            _issuerLayout.CounterMaxLength = Authenticator.IssuerMaxLength;
            _issuerText.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(Authenticator.IssuerMaxLength) });
            _usernameLayout.CounterMaxLength = Authenticator.UsernameMaxLength;
            _usernameText.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(Authenticator.UsernameMaxLength) });
            
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
                if(issuer == "")
                {
                    _issuerLayout.Error = GetString(Resource.String.noIssuer);
                    return;
                }

                var args = new RenameEventArgs(_itemPosition, issuer, _usernameText.Text);
                Rename?.Invoke(this, args);
                Dismiss();
            };

            _usernameText.EditorAction += (_, args) =>
            {
                if(args.ActionId == ImeAction.Done)
                    renameButton.PerformClick();
            };
            
            return view;
        }

        public class RenameEventArgs : EventArgs
        {
            public readonly int ItemPosition;
            public readonly string Issuer;
            public readonly string Username;

            public RenameEventArgs(int itemPosition, string issuer, string username)
            {
                ItemPosition = itemPosition;
                Issuer = issuer;
                Username = username;
            }
        }
    }
}