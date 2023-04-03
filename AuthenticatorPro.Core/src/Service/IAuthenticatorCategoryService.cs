// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Service
{
    public interface IAuthenticatorCategoryService
    {
        public Task<int> AddManyAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task<int> UpdateManyAsync(IEnumerable<AuthenticatorCategory> acs);
        public Task AddAsync(Authenticator authenticator, Category category);
        public Task RemoveAsync(Authenticator authenticator, Category category);
    }
}