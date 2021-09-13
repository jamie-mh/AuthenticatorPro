// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface ICategoryService
    {
        public Task TransferAsync(Category initial, Category next);
        public Task<int> AddManyAsync(IEnumerable<Category> auths);
        public Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Category> auths);
        public Task<int> UpdateManyAsync(IEnumerable<Category> categories);
        public Task DeleteWithCategoryBindingsASync(Category auth);
    }
}