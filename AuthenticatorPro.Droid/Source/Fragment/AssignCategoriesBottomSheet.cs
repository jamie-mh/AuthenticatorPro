using System;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AssignCategoriesBottomSheet : BottomSheet
    {
        public event EventHandler<CategoryClickedEventArgs> CategoryClick;
        public event EventHandler ManageCategoriesClick;
        public event EventHandler Close;

        private ChipGroup _chipGroup;
        
        private int _position;
        private string[] _categoryIds;
        private string[] _categoryNames;
        private string[] _assignedCategoryIds;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _position = Arguments.GetInt("position", -1);
            _categoryIds = Arguments.GetStringArray("categoryIds");
            _categoryNames = Arguments.GetStringArray("categoryNames");
            _assignedCategoryIds = Arguments.GetStringArray("assignedCategoryIds");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetAssignCategories, null);
            SetupToolbar(view, Resource.String.assignCategories);
            _chipGroup = view.FindViewById<ChipGroup>(Resource.Id.chipGroup);
            
            var emptyText = view.FindViewById<TextView>(Resource.Id.textEmpty);

            if(!_categoryIds.Any())
            {
                emptyText.Visibility = ViewStates.Visible;
                _chipGroup.Visibility = ViewStates.Gone;
            }

            for(var i = 0; i < _categoryIds.Length; ++i)
            {
                var chip = (Chip) inflater.Inflate(Resource.Layout.chipChoice, _chipGroup, false);
                chip.Text = _categoryNames[i];
                chip.Checkable = true;
                chip.Clickable = true;

                if(_assignedCategoryIds.Contains(_categoryIds[i]))
                    chip.Checked = true;

                var categoryPos = i;
                chip.Click += (sender, _) =>
                {
                    CategoryClick?.Invoke(sender, new CategoryClickedEventArgs(_position, categoryPos, chip.Checked));
                };

                _chipGroup.AddView(chip);
            }

            var manageCategoriesButton = view.FindViewById<MaterialButton>(Resource.Id.buttonManageCategories);
            
            if(ManageCategoriesClick != null)
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