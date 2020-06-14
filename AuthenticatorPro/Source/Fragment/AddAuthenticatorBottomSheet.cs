using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Shared.Data;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Java.Lang;
using OtpNet;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Fragment
{
    internal class AddAuthenticatorBottomSheet : ExpandedBottomSheetDialogFragment
    {
        public event EventHandler<Authenticator> Add;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;
        private TextInputLayout _secretLayout;
        private TextInputLayout _typeLayout;
        private TextInputLayout _algorithmLayout;
        private TextInputLayout _periodLayout;
        private TextInputLayout _digitsLayout;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;
        private TextInputEditText _secretText;
        private EditText _periodText;
        private EditText _digitsText;

        private AuthenticatorType _type;
        private OtpHashMode _algorithm;

        public string SecretError {
            set => _secretLayout.Error = value;
        }

        public AddAuthenticatorBottomSheet()
        {
            RetainInstance = true;

            _type = AuthenticatorType.Totp;
            _algorithm = OtpHashMode.Sha1;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAddAuthenticator, null);

            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.editIssuerLayout);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editUsernameLayout);
            _secretLayout = view.FindViewById<TextInputLayout>(Resource.Id.editSecretLayout);
            _typeLayout = view.FindViewById<TextInputLayout>(Resource.Id.editTypeLayout);
            _algorithmLayout = view.FindViewById<TextInputLayout>(Resource.Id.editAlgorithmLayout);
            _periodLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPeriodLayout);
            _digitsLayout = view.FindViewById<TextInputLayout>(Resource.Id.editDigitsLayout);

            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.editIssuer);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);
            _secretText = view.FindViewById<TextInputEditText>(Resource.Id.editSecret);
            _digitsText = view.FindViewById<TextInputEditText>(Resource.Id.editDigits);
            _periodText = view.FindViewById<TextInputEditText>(Resource.Id.editPeriod);

            var typeAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authTypes, Resource.Layout.listItemDropdown);
            var typeEditText = (AutoCompleteTextView) _typeLayout.EditText;
            typeEditText.Adapter = typeAdapter;
            typeEditText.SetText((ICharSequence) typeAdapter.GetItem(0), false);
            typeEditText.ItemClick += OnTypeItemClick;

            var algorithmAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authAlgorithms, Resource.Layout.listItemDropdown);
            var algorithmEditText = (AutoCompleteTextView) _algorithmLayout.EditText;
            algorithmEditText.Adapter = algorithmAdapter;
            algorithmEditText.SetText((ICharSequence) algorithmAdapter.GetItem(0), false);
            algorithmEditText.ItemClick += OnAlgorithmItemClick;

            var advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.layoutAdvanced);
            var advancedButton = view.FindViewById<MaterialButton>(Resource.Id.buttonShowAdvanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += (s, e) =>
            {
                Dismiss();
            };

            // When we've finished typing the secret, remove the keyboard so it doesn't block type autocomplete
            _secretText.EditorAction += (sender, args) =>
            {
                if(args.ActionId != ImeAction.Done)
                    return;

                var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(_secretText.WindowToken, HideSoftInputFlags.None);
            };

            var addButton = view.FindViewById<MaterialButton>(Resource.Id.buttonAdd);
            addButton.Click += OnAddButtonClicked;

            return view;
        }

        private void OnTypeItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _periodLayout.Visibility = e.Position == 0
                ? ViewStates.Visible
                : ViewStates.Invisible;

            _type = e.Position switch {
                1 => AuthenticatorType.Hotp,
                _ => AuthenticatorType.Totp
            };
        }

        private void OnAlgorithmItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _algorithm = e.Position switch {
                1 => OtpHashMode.Sha256,
                2 => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1
            };
        }

        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            var isValid = true;

            var issuer = _issuerText.Text.Trim();
            if(issuer == "")
            {
                _issuerLayout.Error = GetString(Resource.String.noIssuer);
                isValid = false;
            }

            var username = _usernameText.Text.Trim();

            var secret = Authenticator.CleanSecret(_secretText.Text);

            if(secret == "")
            {
                _secretLayout.Error = GetString(Resource.String.noSecret);
                isValid = false;
            }
            else if(!Authenticator.IsValidSecret(secret))
            {
                _secretLayout.Error = GetString(Resource.String.secretInvalid);
                isValid = false;
            }

            if(!Int32.TryParse(_digitsText.Text, out var digits) || digits < 6 || digits > 10)
            {
                _digitsLayout.Error = GetString(Resource.String.digitsInvalid);
                isValid = false;
            }

            if(!Int32.TryParse(_periodText.Text, out var period) || period <= 0)
            {
                _periodLayout.Error = GetString(Resource.String.periodToShort);
                isValid = false;
            }

            if(!isValid)
                return;

            var auth = new Authenticator {
                Type = _type,
                Issuer = issuer,
                Username = username,
                Algorithm = _algorithm,
                Counter = 0,
                Digits = digits,
                Period = period,
                Icon = Icon.FindServiceKeyByName(issuer),
                Ranking = 0,
                Secret = secret
            };

            Add?.Invoke(this, auth);
        }
    }
}