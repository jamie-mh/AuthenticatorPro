// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Core;
using AuthenticatorPro.Droid.Shared.Wear;

namespace AuthenticatorPro.WearOS.Cache.View
{
    public class AuthenticatorView : IReadOnlyList<WearAuthenticator>
    {
        private readonly ListCache<WearAuthenticator> _cache;
        private List<WearAuthenticator> _view;
        private string _categoryId;
        private SortMode _sortMode;

        public AuthenticatorView(ListCache<WearAuthenticator> cache, string categoryId, SortMode sortMode)
        {
            _cache = cache;
            _categoryId = categoryId;
            _sortMode = sortMode;
            Update();
        }

        public string CategoryId
        {
            get => _categoryId;
            set
            {
                _categoryId = value;
                Update();
            }
        }

        public SortMode SortMode
        {
            get => _sortMode;
            set
            {
                _sortMode = value;
                Update();
            }
        }

        public IEnumerator<WearAuthenticator> GetEnumerator()
        {
            return _view.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _view.Count;

        public WearAuthenticator this[int index] => _view[index];

        public void Update()
        {
            var view = _cache.GetItems().AsEnumerable();

            if (CategoryId != null)
            {
                view = view.Where(a => a.Categories != null && a.Categories.Any(c => c.CategoryId == CategoryId));

                if (SortMode == SortMode.Custom)
                {
                    view = view.OrderBy(a => a.Categories.First(c => c.CategoryId == CategoryId).Ranking);
                }
            }

            view = SortMode switch
            {
                SortMode.AlphabeticalAscending => view.OrderBy(a => a.Issuer).ThenBy(a => a.Username),
                SortMode.AlphabeticalDescending => view.OrderByDescending(a => a.Issuer)
                    .ThenByDescending(a => a.Username),
                SortMode.CopyCountAscending => view.OrderBy(a => a.CopyCount).ThenBy(a => a.Issuer),
                SortMode.CopyCountDescending => view.OrderByDescending(a => a.CopyCount).ThenBy(a => a.Issuer),
                SortMode.Custom when CategoryId == null => view.OrderBy(a => a.Ranking),
                _ => view
            };

            _view = view.ToList();
        }

        public int FindIndex(Predicate<WearAuthenticator> predicate)
        {
            return _view.FindIndex(predicate);
        }
    }
}