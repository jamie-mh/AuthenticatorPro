using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data.Source;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;

namespace AuthenticatorPro.Fragment
{
    internal class AssignCategoriesBottomSheet : BottomSheet
    {
        public event EventHandler<CategoryClickedEventArgs> CategoryClick;
        public event EventHandler ManageCategoriesClick;
        public event EventHandler Close;

        private readonly int _itemPosition;
        private readonly CategorySource _categorySource;
        private readonly List<string> _checkedCategories;

        private ChipGroup _chipGroup;


        public AssignCategoriesBottomSheet(CategorySource categorySource, int itemPosition, List<string> checkedCategories)
        {
            RetainInstance = true;

            _categorySource = categorySource;
            _checkedCategories = checkedCategories;
            _itemPosition = itemPosition;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAssignCategories, null);
            SetupToolbar(view, Resource.String.assignCategories);
            _chipGroup = view.FindViewById<ChipGroup>(Resource.Id.chipGroup);
            
            var emptyText = view.FindViewById<TextView>(Resource.Id.textEmpty);

            if(_categorySource.GetView().Count == 0)
            {
                emptyText.Visibility = ViewStates.Visible;
                _chipGroup.Visibility = ViewStates.Gone;
            }

            for(var i = 0; i < _categorySource.GetView().Count; ++i)
            {
                var category = _categorySource.Get(i);
                var chip = (Chip) inflater.Inflate(Resource.Layout.chipChoice, _chipGroup, false);
                chip.Text = category.Name;
                chip.Checkable = true;
                chip.Clickable = true;

                if(_checkedCategories.Contains(category.Id))
                    chip.Checked = true;

                var position = i;
                chip.Click += (sender, e) =>
                {
                    CategoryClick?.Invoke(sender, new CategoryClickedEventArgs(_itemPosition, position, chip.Checked));
                };

                _chipGroup.AddView(chip);
            }

            var manageCategoriesButton = view.FindViewById<MaterialButton>(Resource.Id.buttonManageCategories);
            manageCategoriesButton.Click += ManageCategoriesClick;

            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            okButton.Click += (sender, args) =>
            {
                Close?.Invoke(sender, args);
                Dismiss();
            };

            return view;
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