// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class AuthenticatorRepository : AsyncRepository<Authenticator, string>, IAuthenticatorRepository
    {
        private readonly Database _database;
        
        public AuthenticatorRepository(Database database) : base(database)
        {
            _database = database;
        }

        public async Task ChangeSecretAsync(string oldSecret, string newSecret)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync(
                "UPDATE authenticator SET secret = ? WHERE secret = ?", newSecret, oldSecret);
        }
    }
}