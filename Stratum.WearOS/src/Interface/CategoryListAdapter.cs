// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Android.Content;
using Android.Graphics.Drawables;
using AndroidX.Core.Content;
using AndroidX.Wear.Widget.Drawer;
using Java.Lang;
using Stratum.WearOS.Cache.View;

namespace Stratum.WearOS.Interface
{
    public class CategoryListAdapter : WearableNavigationDrawerView.WearableNavigationDrawerAdapter
    {
        private readonly Context _context;
        private readonly CategoryView _categoryView;

        public CategoryListAdapter(Context context, CategoryView categoryView)
        {
            _context = context;
            _categoryView = categoryView;
        }

        public override int Count => _categoryView.Count + 1;

        public override Drawable GetItemDrawable(int pos)
        {
            return ContextCompat.GetDrawable(_context, Resource.Drawable.baseline_menu_24);
        }

        public override ICharSequence GetItemTextFormatted(int pos)
        {
            if (pos == 0)
            {
                return new String(_context.GetString(Resource.String.categoryAll));
            }

            var item = _categoryView[pos - 1];

            return item == null
                ? new String()
                : new String(item.Name);
        }
    }
}