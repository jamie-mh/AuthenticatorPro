using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Shared.Data;
using Google.Android.Material.BottomSheet;

namespace AuthenticatorPro.Fragment
{
    internal class EditMenuBottomSheet : BottomSheet
    {
        public event EventHandler ClickRename;
        public event EventHandler ClickChangeIcon;
        public event EventHandler ClickAssignCategories;
        public event EventHandler ClickDelete;

        private readonly AuthenticatorType _type;
        private readonly long _counter;


        public EditMenuBottomSheet(AuthenticatorType type, long counter)
        {
            RetainInstance = true;
            _type = type;
            _counter = counter;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetEditMenu, container, false);

            if(_type == AuthenticatorType.Hotp)
            {
                var counterText = view.FindViewById<TextView>(Resource.Id.textCounter);
                counterText.Text = _counter.ToString();

                view.FindViewById<LinearLayout>(Resource.Id.layoutCounter).Visibility = ViewStates.Visible;
            }

            var renameItem = view.FindViewById<LinearLayout>(Resource.Id.buttonRename);
            var changeIconItem = view.FindViewById<LinearLayout>(Resource.Id.buttonChangeIcon);
            var assignCategoriesItem = view.FindViewById<LinearLayout>(Resource.Id.buttonAssignCategories);
            var deleteItem = view.FindViewById<LinearLayout>(Resource.Id.buttonDelete);

            renameItem.Click += (sender, e) =>
            {
                ClickRename?.Invoke(sender, e);
                Dismiss();
            };

            changeIconItem.Click += (sender, e) =>
            {
                ClickChangeIcon?.Invoke(sender, e);
                Dismiss();
            };

            assignCategoriesItem.Click += (sender, e) =>
            {
                ClickAssignCategories?.Invoke(sender, e);
                Dismiss();
            };

            deleteItem.Click += (sender, e) =>
            {
                ClickDelete?.Invoke(sender, e);
                Dismiss();
            };

            return view;
        }
    }
}