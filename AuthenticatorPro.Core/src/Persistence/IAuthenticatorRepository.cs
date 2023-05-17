// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Persistence
{
    public interface IAuthenticatorRepository : IAsyncRepository<Authenticator, string>
    {
        public Task ChangeSecretAsync(string oldSecret, string newSecret);
    }
}