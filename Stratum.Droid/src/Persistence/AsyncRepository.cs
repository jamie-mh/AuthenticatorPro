// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;
using SQLite;

namespace Stratum.Droid.Persistence
{
    public abstract class AsyncRepository<T, TU> : IAsyncRepository<T, TU> where T : new()
    {
        private readonly Database _database;

        protected AsyncRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(T item)
        {
            var conn = await _database.GetConnectionAsync();

            try
            {
                await conn.InsertAsync(item);
            }
            catch (SQLiteException e)
            {
                throw new EntityDuplicateException(e);
            }
        }

        public async Task<T> GetAsync(TU id)
        {
            var conn = await _database.GetConnectionAsync();

            try
            {
                return await conn.GetAsync<T>(id);
            }
            catch (InvalidOperationException)
            {
                return default;
            }
        }

        public async Task<List<T>> GetAllAsync()
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<T>().ToListAsync();
        }

        public async Task UpdateAsync(T item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.UpdateAsync(item);
        }

        public async Task DeleteAsync(T item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.DeleteAsync(item);
        }
    }
}