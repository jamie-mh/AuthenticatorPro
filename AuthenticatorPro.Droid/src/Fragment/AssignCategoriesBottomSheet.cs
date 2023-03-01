// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.OS;
using Android.Views;
using Android.Widget;
using AuthenticatorPro.Shared.View;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;
using System;
using System.Linq;

namespace AuthenticatorPro.Droid.Fragment
{
    internal class AssignCategoriesBottomSheet : BottomSheet
    {
        public event EventHandler<CategoryClickedEventArgs> CategoryClicked;
        public event EventHandler EditCategoriesClicked;
        public event EventHandler Closed;

        private readonly ICategoryView _categoryView;

        private string _secret;
        private string[] _assignedCategoryIds;

        public AssignCategoriesBottomSheet() : base(Resource.Layout.sheetAssignCategories)
        {
            _categoryView = Dependencies.Resolve<ICategoryView>();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _secret = Arguments.GetString("secret");
            _assignedCategoryIds = Arguments.GetStringArray("assignedCategoryIds");
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = base.OnCreateView(inflater, container, savedInstanceState);
            SetupToolbar(view, Resource.String.assignCategories);

            var editCategoriesButton = view.FindViewById<MaterialButton>(Resource.Id.buttonEditCategories);

            if (EditCategoriesClicked != null)
            {
                editCategoriesButton.Click += EditCategoriesClicked;
            }

            var okButton = view.FindViewById<MaterialButton>(Resource.Id.buttonOK);
            okButton.Click += (sender, args) =>
            {
                Closed?.Invoke(sender, args);
                Dismiss();
            };

            return view;
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            await _categoryView.LoadFromPersistence();

            var emptyText = View.FindViewById<TextView>(Resource.Id.textEmpty);
            var chipGroup = View.FindViewById<ChipGroup>(Resource.Id.chipGroup);

            if (!_categoryView.Any())
            {
                emptyText.Visibility = ViewStates.Visible;
                chipGroup.Visibility = ViewStates.Gone;
            }

            foreach (var category in _categoryView)
            {
                var chip = (Chip) StyledInflater.Inflate(Resource.Layout.chipChoice, chipGroup, false);
                chip.Text = category.Name;
                chip.Checkable = true;
                chip.Clickable = true;

                if (_assignedCategoryIds.Contains(category.Id))
                {
                    chip.Checked = true;
                }

                chip.Click += (sender, _) =>
                {
                    CategoryClicked?.Invoke(sender, new CategoryClickedEventArgs(_secret, category.Id, chip.Checked));
                };

                chipGroup.AddView(chip);
            }
        }

        public class CategoryClickedEventArgs : EventArgs
        {
            public readonly string Secret;
            public readonly string CategoryId;
            public readonly bool IsChecked;

            public CategoryClickedEventArgs(string secret, string categoryId, bool isChecked)
            {
                Secret = secret;
                CategoryId = categoryId;
                IsChecked = isChecked;
            }
        }
    }
}