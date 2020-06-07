using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using AuthenticatorPro.Data;
using AuthenticatorPro.List;
using AlertDialog = AndroidX.AppCompat.App.AlertDialog;
using DialogFragment = AndroidX.Fragment.App.DialogFragment;

namespace AuthenticatorPro.Dialog
{
    internal class ChooseCategoriesDialog : DialogFragment
    {
        public event EventHandler<CategoryClickedEventArgs> CategoryClick;
        public event EventHandler Close;

        private readonly int _itemPosition;
        private readonly CategorySource _categorySource;
        private readonly List<string> _checkedCategories;

        private ChooseCategoriesListAdapter _categoryListAdapter;
        private RecyclerView _categoryList;


        public ChooseCategoriesDialog(CategorySource categorySource, int itemPosition, List<string> checkedCategories)
        {
            RetainInstance = true;

            _categorySource = categorySource;
            _checkedCategories = checkedCategories;
            _itemPosition = itemPosition;
        }

        public override Android.App.Dialog OnCreateDialog(Bundle savedInstanceState)
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

            _categoryListAdapter = new ChooseCategoriesListAdapter(_categorySource);
            _categoryListAdapter.ItemClick += OnItemClick;

            _categoryList.SetAdapter(_categoryListAdapter);
            _categoryList.HasFixedSize = true;
            _categoryList.SetItemViewCacheSize(20);

            var okButton = dialog.GetButton((int) DialogButtonType.Positive);
            okButton.Click += Close;

            var emptyText = view.FindViewById<TextView>(Resource.Id.dialogChooseCategories_empty);

            if(_categorySource.Categories.Count == 0)
            {
                emptyText.Visibility = ViewStates.Visible;
                _categoryList.Visibility = ViewStates.Gone;
            }

            foreach(var category in _checkedCategories)
            {
                var index = _categorySource.Categories.FindIndex(c => c.Id == category);
                _categoryListAdapter.CheckedStatus[index] = true;
            }

            return dialog;
        }

        private void OnItemClick(object sender, int position)
        {
            var args = new CategoryClickedEventArgs(_itemPosition, position, _categoryListAdapter.CheckedStatus[position]);
            CategoryClick?.Invoke(sender, args);
        }

        public class CategoryClickedEventArgs : EventArgs
        {
            public readonly int ItemPosition;
            public readonly int CategoryPosition;
            public readonly bool IsChecked;

            public CategoryClickedEventArgs(int itemPosition, int categoryPosition, bool isChecked)
            {
                ItemPosition = itemPosition;
                CategoryPosition = categoryPosition;
                IsChecked = isChecked;
            }
        }
    }
}