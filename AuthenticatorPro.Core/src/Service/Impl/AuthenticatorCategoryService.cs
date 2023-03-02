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
    public class AuthenticatorCategoryService : IAuthenticatorCategoryService
    {
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;
        private readonly IEqualityComparer<AuthenticatorCategory> _equalityComparer;

        public AuthenticatorCategoryService(
            IAuthenticatorCategoryRepository authenticatorCategoryRepository,
            IEqualityComparer<AuthenticatorCategory> equalityComparer)
        {
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
            _equalityComparer = equalityComparer;
        }

        public async Task<int> AddManyAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                throw new ArgumentException("Authenticator categories cannot be null");
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

        public async Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                throw new ArgumentException("Authenticator categories cannot be null");
            }

            var list = acs.ToList();
            var added = await AddManyAsync(list);
            var updated = await UpdateManyAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                throw new ArgumentException("Authenticator categories cannot be null");
            }

            var updated = 0;

            foreach (var ac in acs)
            {
                var original = await _authenticatorCategoryRepository.GetAsync(
                    new ValueTuple<string, string>(ac.AuthenticatorSecret, ac.CategoryId));

                if (original == null || _equalityComparer.Equals(original, ac))
                {
                    continue;
                }

                await _authenticatorCategoryRepository.UpdateAsync(ac);
                updated++;
            }

            return updated;
        }

        public async Task AddAsync(Authenticator authenticator, Category category)
        {
            if (authenticator == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

            if (category == null)
            {
                throw new ArgumentException("Category cannot be null");
            }

            await _authenticatorCategoryRepository.CreateAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }

        public async Task RemoveAsync(Authenticator authenticator, Category category)
        {
            if (authenticator == null)
            {
                throw new ArgumentException("Authenticator cannot be null");
            }

            if (category == null)
            {
                throw new ArgumentException("Category cannot be null");
            }

            await _authenticatorCategoryRepository.DeleteAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }
    }
}