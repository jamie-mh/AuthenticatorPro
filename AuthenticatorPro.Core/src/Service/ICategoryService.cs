// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service
{
    public interface ICategoryService
    {
        public Task TransferAsync(Category initial, Category next);
        public Task<int> AddManyAsync(IEnumerable<Category> categories);
        public Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Category> categories);
        public Task<int> UpdateManyAsync(IEnumerable<Category> categories);
        public Task DeleteWithCategoryBindingsASync(Category category);
    }
}