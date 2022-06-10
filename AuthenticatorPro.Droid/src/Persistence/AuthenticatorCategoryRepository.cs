// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class AuthenticatorCategoryRepository : IAuthenticatorCategoryRepository
    {
        private readonly Database _database;

        public AuthenticatorCategoryRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnection();
            var id = new ValueTuple<string, string>(item.AuthenticatorSecret, item.CategoryId);

            if (await GetAsync(id) != null)
            {
                throw new EntityDuplicateException();
            }

            await conn.InsertAsync(item);
        }

        public async Task<AuthenticatorCategory> GetAsync(ValueTuple<string, string> id)
        {
            var conn = await _database.GetConnection();
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
            var conn = await _database.GetConnection();
            return await conn.Table<AuthenticatorCategory>().ToListAsync();
        }

        public async Task UpdateAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync(
                "UPDATE authenticatorcategory SET authenticatorSecret = ?, categoryId = ?, ranking = ? WHERE authenticatorSecret = ? AND categoryId = ?",
                item.AuthenticatorSecret, item.CategoryId, item.Ranking, item.AuthenticatorSecret, item.CategoryId);
        }

        public async Task DeleteAsync(AuthenticatorCategory item)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync(
                "DELETE FROM authenticatorcategory WHERE authenticatorSecret = ? AND categoryId = ?",
                item.AuthenticatorSecret, item.CategoryId);
        }

        public async Task<List<AuthenticatorCategory>> GetAllForAuthenticatorAsync(Authenticator auth)
        {
            var conn = await _database.GetConnection();
            return await conn.Table<AuthenticatorCategory>().Where(ac => ac.AuthenticatorSecret == auth.Secret)
                .ToListAsync();
        }

        public async Task<List<AuthenticatorCategory>> GetAllForCategoryAsync(Category category)
        {
            var conn = await _database.GetConnection();
            return await conn.Table<AuthenticatorCategory>().Where(ac => ac.CategoryId == category.Id).ToListAsync();
        }

        public async Task DeleteAllForAuthenticatorAsync(Authenticator authenticator)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync("DELETE FROM authenticatorcategory WHERE authenticatorSecret = ?",
                authenticator.Secret);
        }

        public async Task DeleteAllForCategoryAsync(Category category)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync("DELETE FROM authenticatorcategory WHERE categoryId = ?", category.Id);
        }

        public async Task TransferAsync(Category initial, Category next)
        {
            var conn = await _database.GetConnection();
            await conn.ExecuteAsync(
                "UPDATE authenticatorcategory SET categoryId = ? WHERE categoryId = ?", next.Id, initial.Id);
        }
    }
}