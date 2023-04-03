// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class CustomIconRepository : ICustomIconRepository
    {
        private readonly Database _database;

        public CustomIconRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(CustomIcon item)
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

        public async Task<CustomIcon> GetAsync(string id)
        {
            var conn = await _database.GetConnection();

            try
            {
                return await conn.GetAsync<CustomIcon>(id);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<CustomIcon>> GetAllAsync()
        {
            var conn = await _database.GetConnection();
            return await conn.Table<CustomIcon>().ToListAsync();
        }

        public async Task UpdateAsync(CustomIcon item)
        {
            var conn = await _database.GetConnection();
            await conn.UpdateAsync(item);
        }

        public async Task DeleteAsync(CustomIcon item)
        {
            var conn = await _database.GetConnection();
            await conn.DeleteAsync(item);
        }
    }
}