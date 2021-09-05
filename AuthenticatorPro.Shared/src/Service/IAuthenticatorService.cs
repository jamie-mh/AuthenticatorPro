// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface IAuthenticatorService
    {
        public Task AddAsync(Authenticator auth);
        public Task<int> UpdateManyAsync(IEnumerable<Authenticator> auths);
        public Task RenameAsync(Authenticator auth, string issuer, string username);
        public Task SetIconAsync(Authenticator auth, string icon);
        public Task SetCustomIconAsync(Authenticator auth, CustomIcon icon);
        public Task<int> AddManyAsync(IEnumerable<Authenticator> auths);
        public Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Authenticator> auths);
        public Task DeleteWithCategoryBindingsAsync(Authenticator auth);
        public Task IncrementCounterAsync(Authenticator auth);
    }
}