// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Persistence
{
    public interface
        IAuthenticatorCategoryRepository : IAsyncRepository<AuthenticatorCategory, ValueTuple<string, string>>
    {
        public Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator authenticator);
        public Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category);
        public Task DeleteAllForAuthenticatorAsync(Authenticator authenticator);
        public Task DeleteAllForCategoryAsync(Category category);
        public Task TransferCategoryAsync(Category initial, Category next);
        public Task TransferAuthenticatorAsync(Authenticator initial, Authenticator next);
    }
}