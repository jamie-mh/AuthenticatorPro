using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class ImportBottomSheet : BottomSheet
    {
        public event EventHandler ClickAuthenticatorPlus;
        public event EventHandler ClickWinAuth;


        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetImport, container, false);
            SetupToolbar(view, Resource.String.importFrom);

            var authenticatorPlusButton = view.FindViewById<LinearLayout>(Resource.Id.buttonAuthenticatorPlus);
            authenticatorPlusButton.Click += (sender, e) => {
                ClickAuthenticatorPlus?.Invoke(sender, e);
                Dismiss();
            };

            var winAuthButton = view.FindViewById<LinearLayout>(Resource.Id.buttonWinAuth);
            winAuthButton.Click += (sender, e) => {
                ClickWinAuth?.Invoke(sender, e);
                Dismiss();
            };
            
            return view;
        }
    }
}