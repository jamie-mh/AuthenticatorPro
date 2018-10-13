using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using DialogFragment = Android.Support.V4.App.DialogFragment;

namespace ProAuth
{
    class AddDialog : DialogFragment
    {
        public int Type => _typeSpinner.SelectedItemPosition;
        public string Issuer => _issuerText.Text;
        public string Username => _usernameText.Text;
        public string Secret => _secretText.Text;
        public int Algorithm => _algorithmSpinner.SelectedItemPosition;
        public int Digits => int.Parse(_digitsText.Text);
        public int Period => int.Parse(_periodText.Text);

        public string IssuerError
        {
            set => _issuerText.Error = value;
        }

        public string SecretError 
        {
            set => _secretText.Error = value;
        }

        public string DigitsError
        {
            set => _digitsText.Error = value;
        }

        public string PeriodError
        {
            set => _periodText.Error = value;
        }

        private EditText _issuerText;
        private EditText _usernameText;
        private EditText _secretText;
        private Spinner _typeSpinner;
        private Spinner _algorithmSpinner;
        private EditText _digitsText;
        private EditText _periodText;

        private readonly Action<object, EventArgs> _positiveButtonEvent;
        private readonly Action<object, EventArgs> _negativeButtonEvent;

        public AddDialog(Action<object, EventArgs> positive, Action<object, EventArgs> negative)
        {
            _positiveButtonEvent = positive;
            _negativeButtonEvent = negative;
        }

        private void FindViews(View view)
        {
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.enterKey);

            alert.SetPositiveButton(Resource.String.add, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogAdd, null);
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogAdd_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogAdd_username);
            _secretText = view.FindViewById<EditText>(Resource.Id.dialogAdd_secret);
            _typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_type);
            _algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_algorithm);
            _digitsText = view.FindViewById<EditText>(Resource.Id.dialogAdd_digits);
            _periodText = view.FindViewById<EditText>(Resource.Id.dialogAdd_period);
            alert.SetView(view);

            ArrayAdapter typeAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authTypes, Android.Resource.Layout.SimpleSpinnerItem);

            ArrayAdapter algorithmAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authAlgorithms, Android.Resource.Layout.SimpleSpinnerItem);

            typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            algorithmAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            Spinner typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_type);
            Spinner algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_algorithm);

            typeSpinner.Adapter = typeAdapter;
            algorithmSpinner.Adapter = algorithmAdapter;

            LinearLayout advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.dialogAdd_advancedOptions);
            Button advancedButton = view.FindViewById<Button>(Resource.Id.dialogAdd_buttonAdvanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            AlertDialog dialog = alert.Create();
            dialog.Show();

            Button addButton = dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += _positiveButtonEvent.Invoke;
            cancelButton.Click += _negativeButtonEvent.Invoke;

            return dialog;
        }
    }
}