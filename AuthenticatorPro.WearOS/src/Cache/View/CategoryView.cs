// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Droid.Shared.Wear;

namespace AuthenticatorPro.WearOS.Cache.View
{
    public class CategoryView : IReadOnlyList<WearCategory>
    {
        private readonly ListCache<WearCategory> _cache;
        private List<WearCategory> _view;

        public CategoryView(ListCache<WearCategory> cache)
        {
            _cache = cache;
            Update();
        }

        public IEnumerator<WearCategory> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _view.Count;

        public WearCategory this[int index] => _view[index];

        public void Update()
        {
            _view = _cache
                .GetItems()
                .OrderBy(c => c.Ranking)
                .ToList();
        }

        public int FindIndex(Predicate<WearCategory> predicate)
        {
            return _view.FindIndex(predicate);
        }
    }
}