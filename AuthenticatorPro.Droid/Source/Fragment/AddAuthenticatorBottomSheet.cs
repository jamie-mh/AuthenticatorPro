using System;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Droid.Shared.Data;
using AuthenticatorPro.Droid.Util;
using AuthenticatorPro.Shared.Source.Data;
using AuthenticatorPro.Shared.Source.Data.Generator;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using Java.Lang;
using String = System.String;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AddAuthenticatorBottomSheet : BottomSheet
    {
        public event EventHandler<Authenticator> Add;

        private LinearLayout _advancedLayout;
        private MaterialButton _advancedButton;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;
        private TextInputLayout _secretLayout;
        private TextInputLayout _pinLayout;
        private TextInputLayout _typeLayout;
        private TextInputLayout _periodLayout;
        private TextInputLayout _digitsLayout;

        private ArrayAdapter _algorithmAdapter;
        private TextInputLayout _algorithmLayout;
        private AutoCompleteTextView _algorithmText;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;
        private TextInputEditText _secretText;
        private TextInputEditText _pinText;
        private EditText _periodText;
        private EditText _digitsText;

        private readonly IIconResolver _iconResolver;
        private AuthenticatorType _type;
        private Algorithm _algorithm;

        public string SecretError {
            set => _secretLayout.Error = value;
        }

        public AddAuthenticatorBottomSheet()
        {
            _iconResolver = new IconResolver();
            _type = AuthenticatorType.Totp;
            _algorithm = Algorithm.Sha1;
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
            
            TextInputUtil.EnableAutoErrorClear(new[] { _issuerLayout, _secretLayout, _pinLayout, _digitsLayout, _periodLayout });

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

            var typeAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.authTypes, Resource.Layout.listItemDropdown);
            var typeEditText = (AutoCompleteTextView) _typeLayout.EditText;
            typeEditText.Adapter = typeAdapter;
            typeEditText.SetText((ICharSequence) typeAdapter.GetItem(0), false);
            typeEditText.ItemClick += OnTypeItemClick;

            _algorithmAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.authAlgorithms, Resource.Layout.listItemDropdown);
            _algorithmText = (AutoCompleteTextView) _algorithmLayout.EditText;
            _algorithmText.Adapter = _algorithmAdapter;
            _algorithmText.ItemClick += OnAlgorithmItemClick;

            _advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.layoutAdvanced);
            _advancedButton = view.FindViewById<MaterialButton>(Resource.Id.buttonShowAdvanced);
            _advancedButton.Click += delegate
            {
                _advancedLayout.Visibility = ViewStates.Visible;
                _advancedButton.Visibility = ViewStates.Gone;
            };

            // When we've finished typing the secret, remove the keyboard so it doesn't skip to advanced options 
            _secretText.EditorAction += (_, args) =>
            {
                if(args.ActionId != ImeAction.Done)
                    return;

                var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(_secretText.WindowToken, HideSoftInputFlags.None);
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate 
            {
                Dismiss();
            };

            var addButton = view.FindViewById<MaterialButton>(Resource.Id.buttonAdd);
            addButton.Click += OnAddButtonClicked;

            ResetAdvancedOptions();
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

            _periodLayout.Visibility = _type.GetGenerationMethod() == GenerationMethod.Time
                ? ViewStates.Visible
                : ViewStates.Invisible;

            _algorithmLayout.Visibility = _type.IsHmacBased()
                ? ViewStates.Visible
                : ViewStates.Gone;

            _pinLayout.Visibility = _type == AuthenticatorType.MobileOtp
                ? ViewStates.Visible
                : ViewStates.Gone;

            _advancedLayout.Visibility = ViewStates.Gone;
            _advancedButton.Visibility = _type != AuthenticatorType.SteamOtp
                ? ViewStates.Visible
                : ViewStates.Gone;
            
            ResetAdvancedOptions();
        }

        private void ResetAdvancedOptions()
        {
            _digitsText.Text = _type.GetDefaultDigits().ToString();
            _periodText.Text = _type.GetDefaultPeriod().ToString();
            _algorithmText.SetText((ICharSequence) _algorithmAdapter.GetItem(0), false);
        }

        private void OnAlgorithmItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _algorithm = e.Position switch {
                1 => Algorithm.Sha256,
                2 => Algorithm.Sha512,
                _ => Algorithm.Sha1
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

            if(!Int32.TryParse(_digitsText.Text, out var digits) || digits < _type.GetMinDigits() || digits > _type.GetMaxDigits())
            {
                var digitsError = String.Format(GetString(Resource.String.digitsInvalid), _type.GetMinDigits(), _type.GetMaxDigits());
                _digitsLayout.Error = digitsError;
                isValid = false;
            }

            if(!Int32.TryParse(_periodText.Text, out var period) || period <= 0)
            {
                _periodLayout.Error = GetString(Resource.String.periodInvalid);
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
                Icon = _iconResolver.FindServiceKeyByName(issuer),
                Ranking = 0,
                Secret = secret
            };

            Add?.Invoke(this, auth);
        }
    }
}