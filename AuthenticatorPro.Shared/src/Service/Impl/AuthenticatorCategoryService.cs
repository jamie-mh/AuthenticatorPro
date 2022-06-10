// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Data.Comparer;
using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service.Impl
{
    public class AuthenticatorCategoryService : IAuthenticatorCategoryService
    {
        private readonly IAuthenticatorCategoryRepository _authenticatorCategoryRepository;

        public AuthenticatorCategoryService(IAuthenticatorCategoryRepository authenticatorCategoryRepository)
        {
            _authenticatorCategoryRepository = authenticatorCategoryRepository;
        }

        public async Task<int> AddManyAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                return 0;
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
            var list = acs.ToList();
            var added = await AddManyAsync(list);
            var updated = await UpdateManyAsync(list);

            return new ValueTuple<int, int>(added, updated);
        }

        public async Task<int> UpdateManyAsync(IEnumerable<AuthenticatorCategory> acs)
        {
            if (acs == null)
            {
                return 0;
            }

            var updated = 0;
            var comparer = new AuthenticatorCategoryComparer();

            foreach (var ac in acs)
            {
                var original = await _authenticatorCategoryRepository.GetAsync(
                    new ValueTuple<string, string>(ac.AuthenticatorSecret, ac.CategoryId));

                if (original == null || comparer.Equals(original, ac))
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
            await _authenticatorCategoryRepository.CreateAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }

        public async Task RemoveAsync(Authenticator authenticator, Category category)
        {
            await _authenticatorCategoryRepository.DeleteAsync(new AuthenticatorCategory
            {
                AuthenticatorSecret = authenticator.Secret, CategoryId = category.Id
            });
        }
    }
}