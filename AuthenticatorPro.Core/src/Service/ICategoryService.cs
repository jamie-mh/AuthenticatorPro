// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Service
{
    public interface ICategoryService
    {
        public Task<Category> GetCategoryByIdAsync(string id);
        public Task TransferAsync(Category initial, Category next);
        public Task AddCategoryAsync(Category category);
        public Task<int> AddManyCategoriesAsync(IEnumerable<Category> categories);
        public Task<ValueTuple<int, int>> AddOrUpdateManyCategoriesAsync(IEnumerable<Category> categories);
        public Task<int> UpdateManyCategoriesAsync(IEnumerable<Category> categories);
        public Task<int> AddManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task<ValueTuple<int, int>> AddOrUpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task<int> UpdateManyBindingsAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task AddBindingAsync(Authenticator authenticator, Category category);
        public Task RemoveBindingAsync(Authenticator authenticator, Category category);
        public Task DeleteWithCategoryBindingsASync(Category category);
        public Task<List<AuthenticatorCategory>> GetBindingsForAuthenticatorAsync(Authenticator authenticator);
        public Task<List<AuthenticatorCategory>> GetBindingsForCategoryAsync(Category category);
        public Task<List<Category>> GetAllCategoriesAsync();
        public Task<List<AuthenticatorCategory>> GetAllBindingsAsync();
    }
}