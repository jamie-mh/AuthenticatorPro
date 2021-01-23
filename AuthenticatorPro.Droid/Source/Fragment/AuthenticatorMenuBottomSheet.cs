using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Droid.List;
using AuthenticatorPro.Shared.Source.Data;
using AuthenticatorPro.Shared.Source.Data.Generator;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AuthenticatorMenuBottomSheet : BottomSheet
    {
        public event EventHandler ClickRename;
        public event EventHandler ClickChangeIcon;
        public event EventHandler ClickAssignCategories;
        public event EventHandler ClickDelete;

        private readonly AuthenticatorType _type;
        private readonly long _counter;


        public AuthenticatorMenuBottomSheet(AuthenticatorType type, long counter)
        {
            RetainInstance = true;
            _type = type;
            _counter = counter;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAuthenticatorMenu, container, false);

            if(_type.GetGenerationMethod() == GenerationMethod.Counter)
            {
                var counterText = view.FindViewById<TextView>(Resource.Id.textCounter);
                counterText.Text = _counter.ToString();

                view.FindViewById<LinearLayout>(Resource.Id.layoutCounter).Visibility = ViewStates.Visible;
            }

            var menu = view.FindViewById<RecyclerView>(Resource.Id.listMenu);
            SetupMenu(menu, new List<SheetMenuItem>
            {
                new(Resource.Drawable.ic_action_edit, Resource.String.rename, ClickRename),
                new(Resource.Drawable.ic_action_image, Resource.String.changeIcon, ClickChangeIcon),
                new(Resource.Drawable.ic_action_category, Resource.String.assignCategories, ClickAssignCategories),
                new(Resource.Drawable.ic_action_delete, Resource.String.delete, ClickDelete, null, true)
            });

            return view;
        }
    }
}