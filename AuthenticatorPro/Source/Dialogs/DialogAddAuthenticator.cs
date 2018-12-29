using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace AuthenticatorPro.Dialogs
{
    internal class DialogAddAuthenticator : DialogFragment
    {
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private Spinner _algorithmSpinner;
        private EditText _digitsText;

        private EditText _issuerText;
        private TextInputLayout _periodInputLayout;
        private EditText _periodText;
        private EditText _secretText;
        private Spinner _typeSpinner;
        private EditText _usernameText;

        public DialogAddAuthenticator(Action<object, EventArgs> positive, Action<object, EventArgs> negative)
        {
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
            set => _issuerText.Error = value;
        }

        public string SecretError {
            set => _secretText.Error = value;
        }

        public string DigitsError {
            set => _digitsText.Error = value;
        }

        public string PeriodError {
            set => _periodText.Error = value;
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.enterKey);

            alert.SetPositiveButton(Resource.String.add, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogAddAuthenticator, null);
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogAddAuthenticator_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogAddAuthenticator_username);
            _secretText = view.FindViewById<EditText>(Resource.Id.dialogAddAuthenticator_secret);
            _typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_type);
            _algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_algorithm);
            _digitsText = view.FindViewById<EditText>(Resource.Id.dialogAddAuthenticator_digits);
            _periodText = view.FindViewById<EditText>(Resource.Id.dialogAddAuthenticator_period);
            _periodInputLayout =
                view.FindViewById<TextInputLayout>(Resource.Id.dialogAddAuthenticator_periodInputLayout);
            alert.SetView(view);

            var typeAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authTypes, Android.Resource.Layout.SimpleSpinnerItem);

            var algorithmAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authAlgorithms, Android.Resource.Layout.SimpleSpinnerItem);

            typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            algorithmAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            var typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_type);
            var algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAddAuthenticator_algorithm);

            typeSpinner.Adapter = typeAdapter;
            algorithmSpinner.Adapter = algorithmAdapter;

            var advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.dialogAddAuthenticator_advancedOptions);
            var advancedButton = view.FindViewById<Button>(Resource.Id.dialogAddAuthenticator_buttonAdvanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            _typeSpinner.ItemSelected += _typeSpinner_ItemSelected;

            var dialog = alert.Create();
            dialog.Show();

            var addButton = dialog.GetButton((int) DialogButtonType.Positive);
            var cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }

        private void _typeSpinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            _periodInputLayout.Visibility = e.Position == 0 ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}