using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Data;
using Google.Android.Material.BottomSheet;
using Google.Android.Material.Chip;

namespace AuthenticatorPro.Fragment
{
    internal class MainMenuBottomSheet : BottomSheetDialogFragment
    {
        public event EventHandler<int> CategoryClick;
        public event EventHandler BackupClick;
        public event EventHandler ManageCategoriesClick;
        public event EventHandler SettingsClick;

        private readonly string _currCategoryId;
        private readonly CategorySource _source;


        public MainMenuBottomSheet(CategorySource source, string currCategoryId)
        {
            RetainInstance = true;
            _source = source;
            _currCategoryId = currCategoryId;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.sheetMainMenu, container, false);

            var chipGroup = view.FindViewById<ChipGroup>(Resource.Id.chipGroup);
            var allChip = view.FindViewById<Chip>(Resource.Id.chipCategoryAll);

            if(_currCategoryId == null)
                allChip.Checked = true;

            allChip.Click += (sender, args) =>
            {
                CategoryClick?.Invoke(this, -1);
            };

            for(var i = 0; i < _source.Categories.Count; ++i)
            {
                var category = _source.Categories[i];
                var chip = (Chip) inflater.Inflate(Resource.Layout.chipChoice, chipGroup, false);
                chip.Text = category.Name;
                chip.Checkable = true;
                chip.Clickable = true;

                if(category.Id == _currCategoryId)
                    chip.Checked = true;

                var position = i;
                chip.Click += (sender, e) =>
                {
                    CategoryClick?.Invoke(this, position);
                };

                chipGroup.AddView(chip);
            }

            var backupButton = view.FindViewById<LinearLayout>(Resource.Id.buttonBackup);
            var manageCategoriesButton = view.FindViewById<LinearLayout>(Resource.Id.buttonManageCategories);
            var settingsButton = view.FindViewById<LinearLayout>(Resource.Id.buttonSettings);

            backupButton.Click += (sender, args) =>
            {
                BackupClick?.Invoke(this, null);
                Dismiss();
            };

            manageCategoriesButton.Click += (sender, args) =>
            {
                ManageCategoriesClick?.Invoke(this, null);
                Dismiss();
            };

            settingsButton.Click += (sender, args) =>
            {
                SettingsClick?.Invoke(this, null);
                Dismiss();
            };
            return view;
        }
    }
}