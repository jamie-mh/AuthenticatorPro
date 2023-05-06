// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class AuthenticatorRepository : AsyncRepository<Authenticator, string>, IAuthenticatorRepository
    {
        public AuthenticatorRepository(Database database) : base(database)
        {
        }
    }
}