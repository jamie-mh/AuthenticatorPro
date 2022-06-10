// Copyright (C) 2022 jmh
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
    internal class CategoryRepository : ICategoryRepository
    {
        private readonly Database _database;

        public CategoryRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(Category item)
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

        public async Task<Category> GetAsync(string id)
        {
            var conn = await _database.GetConnection();

            try
            {
                return await conn.GetAsync<Category>(id);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<Category>> GetAllAsync()
        {
            var conn = await _database.GetConnection();
            return await conn.Table<Category>().ToListAsync();
        }

        public async Task UpdateAsync(Category item)
        {
            var conn = await _database.GetConnection();
            await conn.UpdateAsync(item);
        }

        public async Task DeleteAsync(Category item)
        {
            var conn = await _database.GetConnection();
            await conn.DeleteAsync(item);
        }
    }
}