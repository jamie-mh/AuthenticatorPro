using System;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Generator;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Java.Lang;
using OtpNet;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Fragment
{
    internal class AddAuthenticatorBottomSheet : BottomSheet
    {
        public event EventHandler<Authenticator> Add;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;
        private TextInputLayout _secretLayout;
        private TextInputLayout _pinLayout;
        private TextInputLayout _typeLayout;
        private TextInputLayout _algorithmLayout;
        private TextInputLayout _periodLayout;
        private TextInputLayout _digitsLayout;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;
        private TextInputEditText _secretText;
        private TextInputEditText _pinText;
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
            SetupToolbar(view, Resource.String.add);
            
            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.editIssuerLayout);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editUsernameLayout);
            _secretLayout = view.FindViewById<TextInputLayout>(Resource.Id.editSecretLayout);
            _pinLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPinLayout);
            _typeLayout = view.FindViewById<TextInputLayout>(Resource.Id.editTypeLayout);
            _algorithmLayout = view.FindViewById<TextInputLayout>(Resource.Id.editAlgorithmLayout);
            _periodLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPeriodLayout);
            _digitsLayout = view.FindViewById<TextInputLayout>(Resource.Id.editDigitsLayout);

            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.editIssuer);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);
            _secretText = view.FindViewById<TextInputEditText>(Resource.Id.editSecret);
            _pinText = view.FindViewById<TextInputEditText>(Resource.Id.editPin);
            _digitsText = view.FindViewById<TextInputEditText>(Resource.Id.editDigits);
            _periodText = view.FindViewById<TextInputEditText>(Resource.Id.editPeriod);

            _issuerLayout.CounterMaxLength = Authenticator.IssuerMaxLength;
            _issuerText.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(Authenticator.IssuerMaxLength) });
            _usernameLayout.CounterMaxLength = Authenticator.UsernameMaxLength;
            _usernameText.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(Authenticator.UsernameMaxLength) });
            _pinLayout.CounterMaxLength = MobileOtp.PinLength;
            _pinText.SetFilters(new IInputFilter[]{ new InputFilterLengthFilter(MobileOtp.PinLength) });

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
            advancedButton.Click += delegate
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate 
            {
                Dismiss();
            };

            // When we've finished typing the secret, remove the keyboard so it doesn't block type autocomplete
            _secretText.EditorAction += (_, args) =>
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
            _type = e.Position switch {
                1 => AuthenticatorType.Hotp,
                2 => AuthenticatorType.MobileOtp,
                3 => AuthenticatorType.SteamOtp,
                _ => AuthenticatorType.Totp,
            };

            _periodLayout.Visibility = _type.GetGenerationMethod() == GenerationMethod.Counter
                ? ViewStates.Visible
                : ViewStates.Invisible;

            _algorithmLayout.Visibility = _type.IsHmacBased() && _type != AuthenticatorType.SteamOtp
                ? ViewStates.Visible
                : ViewStates.Gone;

            _pinLayout.Visibility = _type == AuthenticatorType.MobileOtp
                ? ViewStates.Visible
                : ViewStates.Gone;

            _digitsLayout.Visibility = _type != AuthenticatorType.SteamOtp
                ? ViewStates.Visible
                : ViewStates.Gone;
        }

        private void OnAlgorithmItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _algorithm = e.Position switch {
                1 => OtpHashMode.Sha256,
                2 => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1
            };
        }

        private void ClearErrors()
        {
            _issuerLayout.Error = null;
            _secretLayout.Error = null;
            _pinLayout.Error = null;
            _digitsLayout.Error = null;
            _periodLayout.Error = null;
        }

        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            ClearErrors(); 
            var isValid = true;

            var issuer = _issuerText.Text.Trim();
            if(issuer == "")
            {
                _issuerLayout.Error = GetString(Resource.String.noIssuer);
                isValid = false;
            }

            var username = _usernameText.Text.Trim();
            var pin = _pinText.Text.Trim();

            if(_type == AuthenticatorType.MobileOtp)
            {
                if(pin == "")
                {
                    _pinLayout.Error = GetString(Resource.String.noPin);
                    isValid = false;
                }
                else if(pin.Length < MobileOtp.PinLength)
                {
                    _pinLayout.Error = GetString(Resource.String.pinInvalid);
                    isValid = false;
                }
            }

            var secret = _secretText.Text.Trim();
            
            if(secret == "")
            {
                _secretLayout.Error = GetString(Resource.String.noSecret);
                isValid = false;
            }

            if(_type == AuthenticatorType.MobileOtp)
                secret += pin;
            
            secret = Authenticator.CleanSecret(secret, _type);
            
            if(!Authenticator.IsValidSecret(secret, _type))
            {
                _secretLayout.Error = GetString(Resource.String.secretInvalid);
                isValid = false;
            }

            int digits;
            if(_type == AuthenticatorType.SteamOtp)
            {
                digits = SteamOtp.Digits;
            }
            else if(!Int32.TryParse(_digitsText.Text, out digits) || digits < Authenticator.MinDigits || digits > Authenticator.MaxDigits)
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