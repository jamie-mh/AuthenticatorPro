// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AuthenticatorPro.Core;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Generator;
using AuthenticatorPro.Core.Util;
using AuthenticatorPro.Droid.Util;
using Google.Android.Material.Button;
using Google.Android.Material.Dialog;
using Google.Android.Material.TextField;
using Java.Lang;
using Exception = System.Exception;

namespace AuthenticatorPro.Droid.Interface.Fragment
{
    public abstract class InputAuthenticatorBottomSheet : BottomSheet
    {
        private readonly IIconResolver _iconResolver;

        private LinearLayout _advancedLayout;
        private MaterialButton _advancedButton;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;
        private TextInputLayout _secretLayout;
        private TextInputLayout _pinLayout;
        private TextInputLayout _periodLayout;
        private TextInputLayout _digitsLayout;
        private TextInputLayout _counterLayout;

        private ArrayAdapter _typeAdapter;
        private TextInputLayout _typeLayout;
        private MaterialAutoCompleteTextView _typeText;

        private ArrayAdapter _algorithmAdapter;
        private TextInputLayout _algorithmLayout;
        private MaterialAutoCompleteTextView _algorithmText;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;
        private TextInputEditText _secretText;
        private TextInputEditText _pinText;
        private EditText _periodText;
        private EditText _digitsText;
        private EditText _counterText;

        private AuthenticatorType _type;
        private HashAlgorithm _algorithm;

        protected Authenticator InitialAuthenticator;

        protected InputAuthenticatorBottomSheet(int layout, int title) : base(layout, title)
        {
            _iconResolver = Dependencies.Resolve<IIconResolver>();
        }

        public string SecretError
        {
            set => _secretLayout.Error = value;
        }

        public event EventHandler<InputAuthenticatorEventArgs> SubmitClicked;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);

            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.editIssuerLayout);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.editUsernameLayout);
            _secretLayout = view.FindViewById<TextInputLayout>(Resource.Id.editSecretLayout);
            _pinLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPinLayout);
            _typeLayout = view.FindViewById<TextInputLayout>(Resource.Id.editTypeLayout);
            _algorithmLayout = view.FindViewById<TextInputLayout>(Resource.Id.editAlgorithmLayout);
            _periodLayout = view.FindViewById<TextInputLayout>(Resource.Id.editPeriodLayout);
            _digitsLayout = view.FindViewById<TextInputLayout>(Resource.Id.editDigitsLayout);
            _counterLayout = view.FindViewById<TextInputLayout>(Resource.Id.editCounterLayout);

            TextInputUtil.EnableAutoErrorClear(new[]
            {
                _issuerLayout, _secretLayout, _pinLayout, _digitsLayout, _periodLayout, _counterLayout
            });

            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.editIssuer);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.editUsername);
            _secretText = view.FindViewById<TextInputEditText>(Resource.Id.editSecret);
            _pinText = view.FindViewById<TextInputEditText>(Resource.Id.editPin);
            _digitsText = view.FindViewById<TextInputEditText>(Resource.Id.editDigits);
            _periodText = view.FindViewById<TextInputEditText>(Resource.Id.editPeriod);
            _counterText = view.FindViewById<TextInputEditText>(Resource.Id.editCounter);

            _issuerLayout.CounterMaxLength = Authenticator.IssuerMaxLength;
            _issuerText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(Authenticator.IssuerMaxLength) });
            _usernameLayout.CounterMaxLength = Authenticator.UsernameMaxLength;
            _usernameText.SetFilters(
                new IInputFilter[] { new InputFilterLengthFilter(Authenticator.UsernameMaxLength) });

            _typeAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.authTypes,
                Resource.Layout.listItemDropdown);
            _typeText = (MaterialAutoCompleteTextView) _typeLayout.EditText;
            _typeText.Adapter = _typeAdapter;
            _typeText.SetText((ICharSequence) _typeAdapter.GetItem(0), false);
            _typeText.ItemClick += OnTypeItemClick;

            _algorithmAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.authAlgorithms,
                Resource.Layout.listItemDropdown);
            _algorithmText = (MaterialAutoCompleteTextView) _algorithmLayout.EditText;
            _algorithmText.Adapter = _algorithmAdapter;
            _algorithmText.ItemClick += OnAlgorithmItemClick;

            _advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.layoutAdvanced);
            _advancedButton = view.FindViewById<MaterialButton>(Resource.Id.buttonShowAdvanced);
            _advancedButton.Click += OnAdvancedButtonClick;

            // When we've finished typing the secret, remove the keyboard so it doesn't skip to advanced options
            _secretText.EditorAction += (_, args) =>
            {
                if (args.ActionId != ImeAction.Done)
                {
                    return;
                }

                var imm = (InputMethodManager) Activity.GetSystemService(Context.InputMethodService);
                imm.HideSoftInputFromWindow(_secretText.WindowToken, HideSoftInputFlags.None);
            };

            var cancelButton = view.FindViewById<MaterialButton>(Resource.Id.buttonCancel);
            cancelButton.Click += delegate { Dismiss(); };

            var submitBottom = view.FindViewById<MaterialButton>(Resource.Id.buttonSubmit);
            submitBottom.Click += OnAddButtonClicked;

            // Load initial values
            _type = InitialAuthenticator.Type;
            _algorithm = InitialAuthenticator.Algorithm;

            UpdateType();
            UpdateLayoutForType();
            UpdateAlgorithm();

            // Force text cursor to end
            if (InitialAuthenticator.Issuer != null)
            {
                _issuerText.Append(InitialAuthenticator.Issuer);
            }

            _usernameText.Text = InitialAuthenticator.Username;
            _secretText.Text = InitialAuthenticator.Secret;
            _pinText.Text = InitialAuthenticator.Pin;
            _digitsText.Text = InitialAuthenticator.Digits.ToString();
            _periodText.Text = InitialAuthenticator.Period.ToString();
            _counterText.Text = InitialAuthenticator.Counter.ToString();

            return view;
        }

        private void OnTypeItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _type = e.Position switch
            {
                1 => AuthenticatorType.Hotp,
                2 => AuthenticatorType.MobileOtp,
                3 => AuthenticatorType.SteamOtp,
                4 => AuthenticatorType.YandexOtp,
                _ => AuthenticatorType.Totp
            };

            _digitsText.Text = _type.GetDefaultDigits().ToString();
            _periodText.Text = _type.GetDefaultPeriod().ToString();

            _secretLayout.Error = null;
            _pinLayout.Error = null;
            _digitsLayout.Error = null;
            _periodLayout.Error = null;
            _counterLayout.Error = null;

            UpdateLayoutForType();
        }

        private void UpdateLayoutForType()
        {
            _periodLayout.Visibility = _type.GetGenerationMethod() == GenerationMethod.Time
                ? ViewStates.Visible
                : ViewStates.Invisible;

            _algorithmLayout.Visibility = _type.HasVariableAlgorithm()
                ? ViewStates.Visible
                : ViewStates.Gone;

            _pinLayout.Visibility = _type.HasPin()
                ? ViewStates.Visible
                : ViewStates.Gone;

            if (_type.HasPin())
            {
                _pinLayout.CounterMaxLength = _type.GetMaxPinLength();
                _pinText.SetFilters(new IInputFilter[] { new InputFilterLengthFilter(_type.GetMaxPinLength()) });
            }

            _advancedButton.Visibility =
                _advancedLayout.Visibility == ViewStates.Gone && ShouldShowAdvancedOptions(_type)
                    ? ViewStates.Visible
                    : ViewStates.Gone;

            var hasVariableDigits = _type.GetMinDigits() != _type.GetMaxDigits();

            _digitsLayout.Visibility = hasVariableDigits
                ? ViewStates.Visible
                : ViewStates.Gone;

            _periodLayout.Visibility = _type.HasVariablePeriod()
                ? ViewStates.Visible
                : ViewStates.Gone;

            _counterLayout.Visibility = _type.GetGenerationMethod() == GenerationMethod.Counter
                ? ViewStates.Visible
                : ViewStates.Gone;
        }

        private void UpdateType()
        {
            var position = _type switch
            {
                AuthenticatorType.Hotp => 1,
                AuthenticatorType.MobileOtp => 2,
                AuthenticatorType.SteamOtp => 3,
                AuthenticatorType.YandexOtp => 4,
                _ => 0
            };

            _typeText.SetText((ICharSequence) _typeAdapter.GetItem(position), false);
        }

        private void OnAlgorithmItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _algorithm = e.Position switch
            {
                1 => HashAlgorithm.Sha256,
                2 => HashAlgorithm.Sha512,
                _ => HashAlgorithm.Sha1
            };
        }

        private void UpdateAlgorithm()
        {
            var position = _algorithm switch
            {
                HashAlgorithm.Sha256 => 1,
                HashAlgorithm.Sha512 => 2,
                _ => 0
            };

            _algorithmText.SetText((ICharSequence) _algorithmAdapter.GetItem(position), false);
        }

        private void OnAdvancedButtonClick(object sender, EventArgs e)
        {
            if (!ShouldShowAdvancedWarning())
            {
                ShowAdvancedOptions();
                return;
            }

            var builder = new MaterialAlertDialogBuilder(RequireContext());
            builder.SetMessage(Resource.String.advancedOptionsWarning);
            builder.SetTitle(Resource.String.warning);
            builder.SetIcon(Resource.Drawable.baseline_warning_24);
            builder.SetCancelable(true);

            builder.SetPositiveButton(Resource.String.ok, delegate { ShowAdvancedOptions(); });

            builder.SetNegativeButton(Resource.String.cancel, delegate { });

            var dialog = builder.Create();
            dialog.Show();
        }

        private void ShowAdvancedOptions()
        {
            _advancedLayout.Visibility = ViewStates.Visible;
            _advancedButton.Visibility = ViewStates.Gone;
        }

        private void OnAddButtonClicked(object sender, EventArgs args)
        {
            var isValid = true;

            var issuer = _issuerText.Text.Trim();
            if (issuer == "")
            {
                _issuerLayout.Error = GetString(Resource.String.noIssuer);
                isValid = false;
            }

            var username = _usernameText.Text.Trim();
            var pin = _pinText.Text.Trim();

            if (_type.HasPin())
            {
                if (pin == "")
                {
                    _pinLayout.Error = GetString(Resource.String.noPin);
                    isValid = false;
                }
                else if (pin.Length < _type.GetMinPinLength())
                {
                    var error = string.Format(GetString(Resource.String.pinTooShort), _type.GetMinPinLength());
                    _pinLayout.Error = error;
                    isValid = false;
                }
                else if (pin.Length > _type.GetMaxPinLength())
                {
                    var error = string.Format(GetString(Resource.String.pinTooLong), _type.GetMaxPinLength());
                    _pinLayout.Error = error;
                    isValid = false;
                }
            }

            var secret = _secretText.Text.Trim();

            if (secret == "")
            {
                _secretLayout.Error = GetString(Resource.String.noSecret);
                isValid = false;
            }

            secret = SecretUtil.Clean(secret, _type);

            try
            {
                SecretUtil.Validate(secret, _type);
            }
            catch (Exception e)
            {
                Logger.Error(e);
                _secretLayout.Error = GetString(Resource.String.secretInvalid);
                isValid = false;
            }

            if (!int.TryParse(_digitsText.Text, out var digits) || digits < _type.GetMinDigits() ||
                digits > _type.GetMaxDigits())
            {
                var digitsError = string.Format(GetString(Resource.String.digitsInvalid), _type.GetMinDigits(),
                    _type.GetMaxDigits());
                _digitsLayout.Error = digitsError;
                isValid = false;
            }

            if (!int.TryParse(_periodText.Text, out var period) || period <= 0)
            {
                _periodLayout.Error = GetString(Resource.String.periodInvalid);
                isValid = false;
            }

            if (!int.TryParse(_counterText.Text, out var counter) || period <= 0)
            {
                _counterLayout.Error = GetString(Resource.String.counterInvalid);
                isValid = false;
            }

            if (!isValid)
            {
                return;
            }

            var auth = new Authenticator
            {
                Type = _type,
                Issuer = issuer,
                Username = username,
                Algorithm = _algorithm,
                Counter = counter,
                Digits = digits,
                Period = period,
                Icon = _iconResolver.FindServiceKeyByName(issuer),
                Ranking = 0,
                Secret = secret,
                Pin = pin == "" ? null : pin
            };

            SubmitClicked?.Invoke(this, new InputAuthenticatorEventArgs(InitialAuthenticator.Secret, auth));
        }

        protected abstract bool ShouldShowAdvancedOptions(AuthenticatorType type);
        protected abstract bool ShouldShowAdvancedWarning();

        public class InputAuthenticatorEventArgs : EventArgs
        {
            public readonly string InitialSecret;
            public readonly Authenticator Authenticator;

            public InputAuthenticatorEventArgs(string initialSecret, Authenticator authenticator)
            {
                InitialSecret = initialSecret;
                Authenticator = authenticator;
            }
        }
    }
}