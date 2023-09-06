// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;

namespace AuthenticatorPro.Core.Service.Impl
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
            if (id == null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return _categoryRepository.GetAsync(id);
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
            await _authenticatorCategoryRepository.TransferCategoryAsync(initial, next);
            await _categoryRepository.DeleteAsync(initial);
        }

        public Task AddCategoryAsync(Category category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            return _categoryRepository.CreateAsync(category);
        }

        public async Task<int> AddManyCategoriesAsync(IEnumerable<Category> categories)
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

        public async Task<ValueTuple<int, int>> AddOrUpdateManyCategoriesAsync(IEnumerable<Category> categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

            var list = categories.ToList();
            var added = await AddManyCategoriesAsync(list);
            var updated = await UpdateManyCategoriesAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyCategoriesAsync(IEnumerable<Category> categories)
        {
            if (categories == null)
            {
                throw new ArgumentNullException(nameof(categories));
            }

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
            if (acs == null)
            {
                throw new ArgumentNullException(nameof(acs));
            }

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
            if (acs == null)
            {
                throw new ArgumentNullException(nameof(acs));
            }

            var list = acs.ToList();
            var added = await AddManyBindingsAsync(list);
            var updated = await UpdateManyBindingsAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                throw new ArgumentNullException(nameof(acs));
            }

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

        public async Task AddBindingAsync(Authenticator authenticator, Category category)
        {
            if (authenticator == null)
            {
                throw new ArgumentNullException(nameof(authenticator));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            await _authenticatorCategoryRepository.CreateAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }

        public async Task RemoveBindingAsync(Authenticator authenticator, Category category)
        {
            if (authenticator == null)
            {
                throw new ArgumentNullException(nameof(authenticator));
            }

            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

            await _authenticatorCategoryRepository.DeleteAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
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

        public Task<List<AuthenticatorCategory>> GetBindingsForAuthenticatorAsync(Authenticator authenticator)
        {
            if (authenticator == null)
            {
                throw new ArgumentNullException(nameof(authenticator));
            }

            return _authenticatorCategoryRepository.GetAllForAuthenticatorAsync(authenticator);
        }

        public Task<List<AuthenticatorCategory>> GetBindingsForCategoryAsync(Category category)
        {
            if (category == null)
            {
                throw new ArgumentNullException(nameof(category));
            }

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