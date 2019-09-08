using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.CategoryList;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace AuthenticatorPro.Dialogs
{
    internal class ChooseCategoriesDialog : DialogFragment
    {
        private readonly CategorySource _categorySource;

        private readonly List<string> _checkedCategories;
        private readonly Action<bool, int> _itemClick;
        private readonly EventHandler _onClose;
        private ChooseCategoriesAdapter _categoryAdapter;

        private RecyclerView _categoryList;

        public ChooseCategoriesDialog(CategorySource categorySource, EventHandler onClose, Action<bool, int> itemClick,
            int authPosition, List<string> checkedCategories)
        {
            _categorySource = categorySource;
            _itemClick = itemClick;
            _onClose = onClose;
            _checkedCategories = checkedCategories;
            AuthPosition = authPosition;
        }

        public int AuthPosition { get; }

        public override Dialog OnCreateDialog(Bundle savedInstanceState)
        {
            var alert = new AlertDialog.Builder(Activity);
            alert.SetTitle(Resource.String.chooseCategories);
            alert.SetCancelable(false);
            alert.SetPositiveButton(Resource.String.ok, (EventHandler<DialogClickEventArgs>) null);

            var view = Activity.LayoutInflater.Inflate(Resource.Layout.dialogChooseCategories, null);
            _categoryList = view.FindViewById<RecyclerView>(Resource.Id.dialogChooseCategories_list);
            alert.SetView(view);

            var dialog = alert.Create();
            dialog.Show();

            var layout = new LinearLayoutManager(Context);
            var decoration = new DividerItemDecoration(Context, layout.Orientation);
            _categoryList.AddItemDecoration(decoration);
            _categoryList.SetLayoutManager(layout);

            _categoryAdapter = new ChooseCategoriesAdapter(_categorySource);
            _categoryAdapter.ItemClick += _itemClick;

            _categoryList.SetAdapter(_categoryAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetItemViewCacheSize(20);

            var okButton = dialog.GetButton((int) DialogButtonType.Positive);
            okButton.Click += _onClose.Invoke;

            var emptyText = view.FindViewById<TextView>(Resource.Id.dialogChooseCategories_empty);

            if(_categorySource.Count() == 0)
            {
                emptyText.Visibility = ViewStates.Visible;
                _categoryList.Visibility = ViewStates.Gone;
            }

            foreach(var category in _checkedCategories)
            {
                var index = _categorySource.Categories.FindIndex(c => c.Id == category);
                _categoryAdapter.CheckedStatus[index] = true;
            }

            return dialog;
        }
    }
}