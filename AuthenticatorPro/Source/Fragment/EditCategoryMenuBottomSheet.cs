using System;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace AuthenticatorPro.Fragment
{
    internal class EditCategoryMenuBottomSheet: BottomSheet
    {
        public event EventHandler<int> ClickRename;
        public event EventHandler<int> ClickDelete;

        private readonly int _itemPosition;


        public EditCategoryMenuBottomSheet(int itemPosition)
        {
            RetainInstance = true;
            _itemPosition = itemPosition;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetEditCategoryMenu, container, false);

            var renameItem = view.FindViewById<LinearLayout>(Resource.Id.buttonRename);
            var deleteItem = view.FindViewById<LinearLayout>(Resource.Id.buttonDelete);

            renameItem.Click += (sender, _) =>
            {
                ClickRename?.Invoke(sender, _itemPosition);
                Dismiss();
            };

            deleteItem.Click += (sender, _) =>
            {
                ClickDelete?.Invoke(sender, _itemPosition);
                Dismiss();
            };

            return view;
        }
    }
}