// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;

namespace Stratum.Core.Service.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly IEqualityComparer<Category> _categoryComparer;
        private readonly IEqualityComparer<AuthenticatorCategory> _authenticatorCategoryComparer;

        public CategoryService(ICategoryRepository categoryRepository,
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            IEqualityComparer<Category> categoryComparer,
            IEqualityComparer<AuthenticatorCategory> authenticatorCategoryComparer)
        {
            _categoryRepository = categoryRepository;
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _categoryComparer = categoryComparer;
            _authenticatorCategoryComparer = authenticatorCategoryComparer;
        }

        public Task<Category> GetCategoryByIdAsync(string id)
        {
            ArgumentNullException.ThrowIfNull(id);
            return _categoryRepository.GetAsync(id);
        }

        public async Task TransferAsync(Category initial, Category next)
        {
            ArgumentNullException.ThrowIfNull(initial);
            ArgumentNullException.ThrowIfNull(next);

            await _categoryRepository.CreateAsync(next);
            await _authenticatorCategoryRepository.TransferCategoryAsync(initial, next);
            await _categoryRepository.DeleteAsync(initial);
        }

        public Task AddCategoryAsync(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);
            return _categoryRepository.CreateAsync(category);
        }

        public async Task<int> AddManyCategoriesAsync(IEnumerable<Category> categories)
        {
            ArgumentNullException.ThrowIfNull(categories);

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

        public async Task<ValueTuple<int, int>> AddOrUpdateManyCategoriesAsync(IEnumerable<Category> categories)
        {
            ArgumentNullException.ThrowIfNull(categories);

            var list = categories.ToList();
            var added = await AddManyCategoriesAsync(list);
            var updated = await UpdateManyCategoriesAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyCategoriesAsync(IEnumerable<Category> categories)
        {
            ArgumentNullException.ThrowIfNull(categories);

            var updated = 0;

            foreach (var category in categories)
            {
                var original = await _categoryRepository.GetAsync(category.Id);

                if (original == null || _categoryComparer.Equals(original, category))
                {
                    continue;
                }

                await _categoryRepository.UpdateAsync(category);
                updated++;
            }

            return updated;
        }

        public async Task<int> AddManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            ArgumentNullException.ThrowIfNull(acs);

            var added = 0;

            foreach (var ac in acs)
            {
                try
                {
                    await _authenticatorCategoryRepository.CreateAsync(ac);
                }
                catch (EntityDuplicateException)
                {
                    continue;
                }

                added++;
            }

            return added;
        }

        public async Task<(int, int)> AddOrUpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            ArgumentNullException.ThrowIfNull(acs);

            var list = acs.ToList();
            var added = await AddManyBindingsAsync(list);
            var updated = await UpdateManyBindingsAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            ArgumentNullException.ThrowIfNull(acs);

            var updated = 0;

            foreach (var ac in acs)
            {
                var original = await _authenticatorCategoryRepository.GetAsync(
                    new ValueTuple<string, string>(ac.AuthenticatorSecret, ac.CategoryId));

                if (original == null || _authenticatorCategoryComparer.Equals(original, ac))
                {
                    continue;
                }

                await _authenticatorCategoryRepository.UpdateAsync(ac);
                updated++;
            }

            return updated;
        }

        public Task AddBindingAsync(Authenticator authenticator, Category category)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(category);

            return _authenticatorCategoryRepository.CreateAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }

        public Task RemoveBindingAsync(Authenticator authenticator, Category category)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            ArgumentNullException.ThrowIfNull(category);

            return _authenticatorCategoryRepository.DeleteAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }

        public async Task DeleteWithCategoryBindingsASync(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);
            await _categoryRepository.DeleteAsync(category);
            await _authenticatorCategoryRepository.DeleteAllForCategoryAsync(category);
        }

        public Task<List<AuthenticatorCategory>> GetBindingsForAuthenticatorAsync(Authenticator authenticator)
        {
            ArgumentNullException.ThrowIfNull(authenticator);
            return _authenticatorCategoryRepository.GetAllForAuthenticatorAsync(authenticator);
        }

        public Task<List<AuthenticatorCategory>> GetBindingsForCategoryAsync(Category category)
        {
            ArgumentNullException.ThrowIfNull(category);
            return _authenticatorCategoryRepository.GetAllForCategoryAsync(category);
        }

        public Task<List<Category>> GetAllCategoriesAsync()
        {
            return _categoryRepository.GetAllAsync();
        }

        public Task<List<AuthenticatorCategory>> GetAllBindingsAsync()
        {
            return _authenticatorCategoryRepository.GetAllAsync();
        }
    }
}