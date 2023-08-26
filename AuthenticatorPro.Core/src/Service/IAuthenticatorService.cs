// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Service
{
    public interface IAuthenticatorService
    {
        public Task AddAsync(Authenticator auth);
        public Task UpdateAsync(Authenticator auth);
        public Task<int> UpdateManyAsync(IEnumerable<Authenticator> auths);
        public Task ChangeSecretAsync(Authenticator auth, string newSecret);
        public Task SetIconAsync(Authenticator auth, string icon);
        public Task SetCustomIconAsync(Authenticator auth, CustomIcon icon);
        public Task<int> AddManyAsync(IEnumerable<Authenticator> auths);
        public Task<ValueTuple<int, int>> AddOrUpdateManyAsync(IEnumerable<Authenticator> auths);
        public Task DeleteWithCategoryBindingsAsync(Authenticator auth);
        public Task IncrementCounterAsync(Authenticator auth);
        public Task IncrementCopyCountAsync(Authenticator auth);
        public Task ResetCopyCountsAsync();
    }
}