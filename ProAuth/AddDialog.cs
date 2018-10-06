using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using OtpSharp;
using ProAuth.Data;
using ProAuth.Utilities;

namespace ProAuth
{
    class AddDialog : DialogFragment
    {
        private AlertDialog _dialog;
        private Database _database;

        private EditText _issuerText;
        private EditText _usernameText;
        private EditText _secretText;
        private Spinner _typeSpinner;
        private Spinner _algorithmSpinner;
        private EditText _digitsText;
        private EditText _periodText;

        public AddDialog(Database database)
        {
            _database = database;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        private void FindViews(View view)
        {
            _issuerText = view.FindViewById<EditText>(Resource.Id.dialogAdd_issuer);
            _usernameText = view.FindViewById<EditText>(Resource.Id.dialogAdd_username);
            _secretText = view.FindViewById<EditText>(Resource.Id.dialogAdd_secret);
            //_typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_type);
            _algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_algorithm);
            _digitsText = view.FindViewById<EditText>(Resource.Id.dialogAdd_digits);
            _periodText = view.FindViewById<EditText>(Resource.Id.dialogAdd_period);
        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.enterKey);

            alert.SetPositiveButton(Resource.String.add, (EventHandler<DialogClickEventArgs>) null);
            alert.SetNegativeButton(Resource.String.cancel, (EventHandler<DialogClickEventArgs>) null);
            alert.SetCancelable(false);

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogAdd, null);
            FindViews(view);
            alert.SetView(view);

            // Fill type and algorithm spinners
            ArrayAdapter typeAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authTypes, Android.Resource.Layout.SimpleSpinnerItem);

            ArrayAdapter algorithmAdapter = ArrayAdapter.CreateFromResource(
                view.Context, Resource.Array.authAlgorithms, Android.Resource.Layout.SimpleSpinnerItem);

            //typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            algorithmAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            //Spinner typeSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_type);
            Spinner algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.dialogAdd_algorithm);

            //typeSpinner.Adapter = typeAdapter;
            algorithmSpinner.Adapter = algorithmAdapter;

            // Advanced options show
            LinearLayout advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.dialogAdd_advancedOptions);
            Button advancedButton = view.FindViewById<Button>(Resource.Id.dialogAdd_buttonAdvanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            _dialog = alert.Create();
            _dialog.Show();

            // Button listeners
            Button addButton = _dialog.GetButton((int) DialogButtonType.Positive);
            Button cancelButton = _dialog.GetButton((int) DialogButtonType.Negative);

            addButton.Click += AddClick;
            cancelButton.Click += CancelClick;

            return _dialog;
        }

        private void AddClick(object sender, EventArgs e)
        {
            if(_issuerText.Text.Trim() == "")
            {
                Toast.MakeText(_dialog.Context, Resource.String.noIssuer, ToastLength.Short).Show();
                return;
            }

            if(_secretText.Text.Trim() == "")
            {
                Toast.MakeText(_dialog.Context, Resource.String.noSecret, ToastLength.Short).Show();
                return;
            }

            if(_secretText.Text.Trim().Length > 32)
            {
                Toast.MakeText(_dialog.Context, Resource.String.secretTooLong, ToastLength.Short).Show();
                return;
            }

            int digits = int.Parse(_digitsText.Text);

            if(digits < 1)
            {
                Toast.MakeText(_dialog.Context, Resource.String.digitsToSmall, ToastLength.Short).Show();
                return;
            }

            int period = int.Parse(_periodText.Text);

            if(period < 1)
            {
                Toast.MakeText(_dialog.Context, Resource.String.periodToShort, ToastLength.Short).Show();
                return;
            }

            string issuer = StringExt.Truncate(_issuerText.Text.Trim(), 32);
            string username = StringExt.Truncate(_usernameText.Text.Trim(), 32);
            string secret = _secretText.Text.Trim();

            OtpHashMode algorithm = OtpHashMode.Sha1;
            switch(_algorithmSpinner.SelectedItemPosition)
            {
                //case 0:
                //    algorithm = OtpHashMode.Sha1;
                //    break;
                case 1:
                    algorithm = OtpHashMode.Sha256;
                    break;
                case 2:
                    algorithm = OtpHashMode.Sha512;
                    break;
            }

            Authenticator auth = new Authenticator() {
                Issuer = issuer,
                Username = username,
                Type = OtpType.Totp,
                Algorithm = algorithm,
                Secret = secret,
                Digits = digits,
                Period = period
            };
            _database.Connection.Insert(auth);

            _dialog?.Dismiss();
        }

        private void CancelClick(object sender, EventArgs e)
        {
            _dialog?.Dismiss();
        }
    }
}