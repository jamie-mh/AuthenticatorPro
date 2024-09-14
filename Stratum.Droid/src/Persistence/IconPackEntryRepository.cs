// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;
using Stratum.Core.Persistence;
using Stratum.Core.Persistence.Exception;

namespace Stratum.Droid.Persistence
{
    public class IconPackEntryRepository : IIconPackEntryRepository
    {
        private readonly Database _database;

        public IconPackEntryRepository(Database database)
        {
            _database = database;
        }

        public async Task CreateAsync(IconPackEntry item)
        {
            var conn = await _database.GetConnectionAsync();
            var id = new ValueTuple<string, string>(item.IconPackName, item.Name);

            if (await GetAsync(id) != null)
            {
                throw new EntityDuplicateException();
            }

            await conn.InsertAsync(item);
        }

        public async Task CreateManyAsync(List<IconPackEntry> items)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.InsertAllAsync(items);
        }

        public async Task<IconPackEntry> GetAsync(ValueTuple<string, string> id)
        {
            var conn = await _database.GetConnectionAsync();
            var (packName, name) = id;

            try
            {
                return await conn.GetAsync<IconPackEntry>(e => e.IconPackName == packName && e.Name == name);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        public async Task<List<IconPackEntry>> GetAllAsync()
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<IconPackEntry>().ToListAsync();
        }

        public async Task UpdateAsync(IconPackEntry item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "UPDATE iconpackentry SET iconPackName = ?, name = ?, data = ? WHERE iconPackName = ? AND name = ?",
                item.IconPackName, item.Name, item.Data, item.IconPackName, item.Name);
        }

        public async Task DeleteAsync(IconPackEntry item)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync(
                "DELETE FROM iconpackentry WHERE iconPackName = ? AND name = ?", item.IconPackName, item.Name);
        }

        public async Task<List<IconPackEntry>> GetAllForPackAsync(IconPack pack)
        {
            var conn = await _database.GetConnectionAsync();
            return await conn.Table<IconPackEntry>().Where(e => e.IconPackName == pack.Name).ToListAsync();
        }

        public async Task DeleteAllForPackAsync(IconPack pack)
        {
            var conn = await _database.GetConnectionAsync();
            await conn.ExecuteAsync("DELETE FROM iconpackentry WHERE iconPackName = ?", pack.Name);
        }
    }
}