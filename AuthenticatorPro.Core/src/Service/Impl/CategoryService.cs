// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly IEqualityComparer<Category> _equalityComparer;

        public CategoryService(ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            IEqualityComparer<Category> equalityComparer)
        {
            _categoryRepository = categoryRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _equalityComparer = equalityComparer;
        }

        public async Task TransferAsync(Category initial, Category next)
        {
            if (initial == null)
            {
                throw new ArgumentNullException(nameof(initial));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            await _categoryRepository.CreateAsync(next);
            await _authenticatorCategoryRepository.TransferAsync(initial, next);
            await _categoryRepository.DeleteAsync(initial);
        }

        public async Task<int> AddManyAsync(IEnumerable<Category> categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            var added = 0;

            foreach (var category in categories)
            {
                try
                {
                    await _categoryRepository.CreateAsync(category);
                }
                catch (EntityDuplicateException)
                {
                    continue;
                }

                added++;
            }

            return added;
        }

        public async Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Category> categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            var list = categories.ToList();
            var added = await AddManyAsync(list);
            var updated = await UpdateManyAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<Category> categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            var updated = 0;

            foreach (var category in categories)
            {
                var original = await _categoryRepository.GetAsync(category.Id);

                if (original == null || _equalityComparer.Equals(original, category))
                {
                    continue;
                }

                await _categoryRepository.UpdateAsync(category);
                updated++;
            }

            return updated;
        }

        public async Task DeleteWithCategoryBindingsASync(Category category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            await _categoryRepository.DeleteAsync(category);
            await _authenticatorCategoryRepository.DeleteAllForCategoryAsync(category);
        }
    }
}