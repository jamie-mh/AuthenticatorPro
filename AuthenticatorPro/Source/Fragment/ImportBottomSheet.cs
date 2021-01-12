using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class ImportBottomSheet : BottomSheet
    {
        public event EventHandler ClickGoogleAuthenticator;
        public event EventHandler ClickAuthenticatorPlus;
        public event EventHandler ClickWinAuth;
        public event EventHandler ClickSteam;
        public event EventHandler ClickBlizzardAuthenticator;
        

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetImport, container, false);
            SetupToolbar(view, Resource.String.importFrom);

            var items = new Dictionary<int, EventHandler>
            {
                { Resource.Id.buttonGoogleAuthenticator, ClickGoogleAuthenticator },
                { Resource.Id.buttonAuthenticatorPlus, ClickAuthenticatorPlus },
                { Resource.Id.buttonWinAuth, ClickWinAuth },
                { Resource.Id.buttonSteam, ClickSteam },
                { Resource.Id.buttonBlizzardAuthenticator, ClickBlizzardAuthenticator }
            };

            foreach(var (res, handler) in items)
            {
                var button = view.FindViewById<LinearLayout>(res);
                button.Click += (sender, args) =>
                {
                    handler?.Invoke(sender, args);
                    Dismiss();
                };
            }

            return view;
        }
    }
}