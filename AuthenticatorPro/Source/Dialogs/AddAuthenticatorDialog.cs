using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.TextField;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using TextInputLayout = Google.Android.Material.TextField.TextInputLayout;

namespace AuthenticatorPro.Dialogs
{
    internal class AddAuthenticatorDialog : AppCompatDialogFragment 
    {
        private readonly Action<object, EventArgs> _negativeButtonEvent;
        private readonly Action<object, EventArgs> _positiveButtonEvent;

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

        public AddAuthenticatorDialog(Action<object, EventArgs> positive, Action<object, EventArgs> negative)
        {
            RetainInstance = true;

            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        public int Type => _typeSpinner.SelectedItemPosition;
        public string Issuer => _issuerText.Text;
        public string Username => _usernameText.Text;
        public string Secret => _secretText.Text;
        public int Algorithm => _algorithmSpinner.SelectedItemPosition;
        public int Digits => Int32.Parse(_digitsText.Text);
        public int Period => Int32.Parse(_periodText.Text);

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

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
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

            _typeSpinner.ItemSelected += _typeSpinner_ItemSelected;

            var dialog = alert.Create();
            dialog.Show();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }

        private void _typeSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _periodLayout.Visibility = e.Position == 0 ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}