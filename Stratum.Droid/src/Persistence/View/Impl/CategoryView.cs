// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;

namespace Stratum.Droid.Persistence.View.Impl
{
    public class CategoryView : ICategoryView
    {
        private readonly ICategoryRepository _categoryRepository;
        private List<Category> _view;

        public CategoryView(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
            _view = new List<Category>();
        }

        public void Swap(int oldPosition, int newPosition)
        {
            var atOldPosition = _view.ElementAtOrDefault(newPosition);

            if (atOldPosition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(oldPosition), "No category at position");
            }

            var atNewPosition = _view.ElementAtOrDefault(oldPosition);

            if (atNewPosition == null)
            {
                throw new ArgumentOutOfRangeException(nameof(newPosition), "No category at position");
            }

            _view[newPosition] = atNewPosition;
            _view[oldPosition] = atOldPosition;
        }

        public IEnumerator<Category> GetEnumerator()
        {
            return _view.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _view).GetEnumerator();
        }

        public int Count => _view.Count;

        public Category this[int index] => _view[index];

        public async Task LoadFromPersistenceAsync()
        {
            _view = await _categoryRepository.GetAllAsync();
            Update();
        }

        public int IndexOf(string id)
        {
            return _view.FindIndex(c => c.Id == id);
        }

        public void Update()
        {
            _view = _view.OrderBy(c => c.Ranking).ToList();
        }
    }
}