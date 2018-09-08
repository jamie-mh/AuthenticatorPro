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
using ProAuth.Data;

namespace ProAuth
{
    public class AddFragment : DialogFragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

        }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.enter_a_provided_key);
            alert.SetPositiveButton(Resource.String.add, (senderAlert, args) =>
            {

            });

            View view = Activity.LayoutInflater.Inflate(Resource.Layout.fragment_add, null);

            LinearLayout advancedLayout = view.FindViewById<LinearLayout>(Resource.Id.advanced_options);
            Button advancedButton = view.FindViewById<Button>(Resource.Id.button_advanced);
            advancedButton.Click += (sender, e) =>
            {
                advancedLayout.Visibility = ViewStates.Visible;
                advancedButton.Visibility = ViewStates.Gone;
            };

            ArrayAdapter typeAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.types,
                Android.Resource.Layout.SimpleSpinnerItem);
            ArrayAdapter algorithmAdapter = ArrayAdapter.CreateFromResource(view.Context, Resource.Array.algorithms,
                Android.Resource.Layout.SimpleSpinnerItem);

            typeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            algorithmAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);

            Spinner typeSpinner = view.FindViewById<Spinner>(Resource.Id.type);
            Spinner algorithmSpinner = view.FindViewById<Spinner>(Resource.Id.algorithm);

            typeSpinner.Adapter = typeAdapter;
            algorithmSpinner.Adapter = algorithmAdapter;

            alert.SetView(view);

            alert.SetNegativeButton(Resource.String.cancel, (senderAlert, args) => {

            });

            return alert.Create();
        }
    }
}