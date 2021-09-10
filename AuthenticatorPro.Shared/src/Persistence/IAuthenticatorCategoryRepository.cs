// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Persistence
{
    public interface
        IAuthenticatorCategoryRepository : IAsyncRepository<AuthenticatorCategory, ValueTuple<string, string>>
    {
        public Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator authenticator);
        public Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category);
        public Task DeleteAllForAuthenticatorAsync(Authenticator authenticator);
        public Task DeleteAllForCategoryAsync(Category category);
        public Task TransferAsync(Category initial, Category next);
    }
}