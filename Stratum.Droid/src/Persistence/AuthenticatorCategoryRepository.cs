// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;

namespace Stratum.Droid.Persistence
{
    public class AuthenticatorCategoryRepository : IAuthenticatorCategoryRepository
    {
        private readonly Database _database;

        public AuthenticatorCategoryRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnectionAsync();
            var id = new ValueTuple<string, string>(item.AuthenticatorSecret, item.CategoryId);

            if (await GetAsync(id) != null)
            {
                throw new EntityDuplicateException();
            }

            await conn.InsertAsync(item);
        }

        public async Task<AuthenticatorCategory> GetAsync(ValueTuple<string, string> id)
        {
            var conn = await _database.GetConnectionAsync();
            var (authSecret, categoryId) = id;

            try
            {
                return await conn.GetAsync<AuthenticatorCategory>(ac =>
                    ac.AuthenticatorSecret == authSecret && ac.CategoryId == categoryId);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<AuthenticatorCategory>> GetAllAsync()
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<AuthenticatorCategory>().ToListAsync();
        }

        public async Task UpdateAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE authenticatorcategory SET authenticatorSecret = ?, categoryId = ?, ranking = ? WHERE authenticatorSecret = ? AND categoryId = ?",
                item.AuthenticatorSecret, item.CategoryId, item.Ranking, item.AuthenticatorSecret, item.CategoryId);
        }

        public async Task DeleteAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ? AND categoryId = ?",
                item.AuthenticatorSecret, item.CategoryId);
        }

        public async Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator auth)
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<AuthenticatorCategory>().Where(ac => ac.AuthenticatorSecret == auth.Secret)
                .ToListAsync();
        }

        public async Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category)
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<AuthenticatorCategory>().Where(ac => ac.CategoryId == category.Id).ToListAsync();
        }

        public async Task DeleteAllForAuthenticatorAsync(Authenticator authenticator)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?",
                authenticator.Secret);
        }

        public async Task DeleteAllForCategoryAsync(Category category)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM authenticatorcategory WHERE categoryId = ?", category.Id);
        }

        public async Task TransferCategoryAsync(Category initial, Category next)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", next.Id, initial.Id);
        }

        public async Task TransferAuthenticatorAsync(Authenticator initial, Authenticator next)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE authenticatorcategory SET authenticatorSecret = ? WHERE authenticatorSecret = ?", next.Secret,
                initial.Secret);
        }
    }
}