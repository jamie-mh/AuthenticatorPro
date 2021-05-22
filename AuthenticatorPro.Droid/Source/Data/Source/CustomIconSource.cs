// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthenticatorPro.Shared.Data;
using SQLite;

namespace AuthenticatorPro.Droid.Data.Source
{
    internal class CustomIconSource
    {
        private readonly SQLiteAsyncConnection _connection;
        private List<CustomIcon> _all;

        public CustomIconSource(SQLiteAsyncConnection connection)
        {
            _connection = connection;
            _all = new List<CustomIcon>();
        }

        public async Task Update()
        {
            _all = await _connection.Table<CustomIcon>().ToListAsync();
        }

        public async Task Add(CustomIcon icon)
        {
            if(Exists(icon.Id))
                throw new ArgumentException("Custom icon already exists");

            await _connection.InsertAsync(icon);
            await Update();
        }

        public async Task<int> AddMany(IEnumerable<CustomIcon> icons)
        {
            var valid = icons.Where(c => !Exists(c.Id)).ToList();
            var added = await _connection.InsertAllAsync(valid);
            await Update();
            return added;
        }

        public CustomIcon Get(string id)
        {
            return _all.FirstOrDefault(i => i.Id == id);
        }

        public async Task Delete(string id)
        {
            await _connection.DeleteAsync<CustomIcon>(id);
            _all.Remove(_all.First(i => i.Id == id));
        }

        public async Task CullUnused()
        {
            // Cannot query as string directly for some reason
            var distinctIconUses = await _connection.QueryAsync<Authenticator>("SELECT DISTINCT icon FROM authenticator");
            var custom = distinctIconUses.Where(i => i.Icon != null && i.Icon.StartsWith(CustomIcon.Prefix.ToString())).Select(i => i.Icon[1..]);
            var unused = _all.Select(i => i.Id).Except(custom).ToList();

            if(!unused.Any())
                return;

            await _connection.RunInTransactionAsync(conn =>
            {
                foreach(var icon in unused)
                    conn.Delete<CustomIcon>(icon);
            });

            await Update();
        }

        private bool Exists(string id)
        {
            return Get(id) != null;
        }

        public List<CustomIcon> GetAll()
        {
            return _all;
        }
    }
}