// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class AuthenticatorRepository : IAuthenticatorRepository
    {
        private readonly Database _database;

        public AuthenticatorRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(Authenticator item)
        {
            var conn = await _database.GetConnection();

            try
            {
                await conn.InsertAsync(item);
            }
            catch (SQLiteException e)
            {
                throw new EntityDuplicateException(e);
            }
        }

        public async Task<Authenticator> GetAsync(string id)
        {
            var conn = await _database.GetConnection();

            try
            {
                return await conn.GetAsync<Authenticator>(id);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<Authenticator>> GetAllAsync()
        {
            var conn = await _database.GetConnection();
            return await conn.Table<Authenticator>().ToListAsync();
        }

        public async Task UpdateAsync(Authenticator item)
        {
            var conn = await _database.GetConnection();
            await conn.UpdateAsync(item);
        }

        public async Task DeleteAsync(Authenticator item)
        {
            var conn = await _database.GetConnection();
            await conn.DeleteAsync(item);
        }
    }
}