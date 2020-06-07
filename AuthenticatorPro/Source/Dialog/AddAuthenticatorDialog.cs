using System;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AuthenticatorPro.Shared;
using AuthenticatorPro.Shared.Data;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using OtpNet;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Dialog
{
    internal class AddAuthenticatorDialog : AppCompatDialogFragment 
    {
        public event EventHandler<AddAuthenticatorEventArgs> Add;

        private TextInputLayout _issuerLayout;
        private TextInputLayout _usernameLayout;
        private TextInputLayout _secretLayout;
        private TextInputLayout _periodLayout;
        private TextInputLayout _digitsLayout;

        private TextInputEditText _issuerText;
        private TextInputEditText _usernameText;
        private TextInputEditText _secretText;
        private Spinner _typeSpinner;
        private Spinner _algorithmSpinner;
        private EditText _periodText;
        private EditText _digitsText;


        public AddAuthenticatorDialog()
        {
            RetainInstance = true;
        }

        public string IssuerError {
            set => _issuerLayout.Error = value;
        }

        public string SecretError {
            set => _secretLayout.Error = value;
        }

        public string UsernameError {
            set => _usernameLayout.Error = value;
        }

        public string DigitsError {
            set => _digitsLayout.Error = value;
        }

        public string PeriodError {
            set => _periodLayout.Error = value;
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.enterKey);

            alert.SetPositiveButton(Resource.String.add, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogAddAuthenticator, null);

            _issuerLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_issuerLayout);
            _usernameLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_usernameLayout);
            _secretLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_secretLayout);
            _periodLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_periodLayout);
            _digitsLayout = view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_digitsLayout);

            _issuerText = view.FindViewById<TextInputEditText>(Resource.Id.dialogAddAuthenticator_issuer);
            _usernameText = view.FindViewById<TextInputEditText>(Resource.Id.dialogAddAuthenticator_username);
            _secretText = view.FindViewById<TextInputEditText>(Resource.Id.dialogAddAuthenticator_secret);
            _typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_type);
            _algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_algorithm);
            _digitsText = view.FindViewById<TextInputEditText>(Resource.Id.dialogAddAuthenticator_digits);
            _periodText = view.FindViewById<TextInputEditText>(Resource.Id.dialogAddAuthenticator_period);
            alert.SetView(view);

            var typeAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authTypes, Android.Resource.Layout.SimpleSpinnerItem);

            var algorithmAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authAlgorithms, Android.Resource.Layout.SimpleSpinnerItem);

            typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            algorithmAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            _typeSpinner.Adapter = typeAdapter;
            _algorithmSpinner.Adapter = algorithmAdapter;

            var advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.dialogAddAuthenticator_advancedOptions);
            var advancedButton = view.FindViewById<MaterialButton>(Resource.Id.dialogAddAuthenticator_buttonAdvanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            _typeSpinner.ItemSelected += OnTypeSpinnerItemSelected;

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += OnAddButtonClicked;

            cancelButton.Click += (sender, e) =>
            {
                dialog.Dismiss();
            };

            return dialog;
        }

        private void OnTypeSpinnerItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _periodLayout.Visibility = e.Position == 0
                ? ViewStates.Visible
                : ViewStates.Gone;
        }

        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            AuthenticatorType type = _typeSpinner.SelectedItemPosition switch {
                1 => AuthenticatorType.Hotp,
                _ => AuthenticatorType.Totp
            };

            OtpHashMode algorithm = _algorithmSpinner.SelectedItemPosition switch {
                1 => OtpHashMode.Sha256,
                2 => OtpHashMode.Sha512,
                _ => OtpHashMode.Sha1
            };

            var args = new AddAuthenticatorEventArgs(
                type, _issuerText.Text, _usernameText.Text, _secretText.Text, algorithm,
                Int32.Parse(_digitsText.Text), Int32.Parse(_periodText.Text)
            );

            Add?.Invoke(this, args);
        }

        public class AddAuthenticatorEventArgs : EventArgs
        {
            public readonly AuthenticatorType Type;
            public readonly string Issuer;
            public readonly string Username;
            public readonly string Secret;
            public readonly OtpHashMode Algorithm;
            public readonly int Digits;
            public readonly int Period;

            public AddAuthenticatorEventArgs(
                AuthenticatorType type,
                string issuer,
                string username,
                string secret,
                OtpHashMode algorithm,
                int digits,
                int period)
            {
                Type = type;
                Issuer = issuer;
                Username = username;
                Secret = secret;
                Algorithm = algorithm;
                Digits = digits;
                Period = period;
            }
        }
    }
}