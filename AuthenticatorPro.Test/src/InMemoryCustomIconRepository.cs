// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;
using AuthenticatorPro.Core.Persistence.Exception;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Test
{
    public class InMemoryCustomIconRepository : ICustomIconRepository
    {
        private readonly List<CustomIcon> _customIcons = new();

        public Task CreateAsync(CustomIcon item)
        {
            if (_customIcons.Any(a => a.Id == item.Id))
            {
                throw new EntityDuplicateException();
            }

            _customIcons.Add(item.Clone());
            return Task.CompletedTask;
        }

        public Task<CustomIcon> GetAsync(string id)
        {
            return Task.FromResult(_customIcons.SingleOrDefault(a => a.Id == id));
        }

        public Task<List<CustomIcon>> GetAllAsync()
        {
            return Task.FromResult(_customIcons);
        }

        public Task UpdateAsync(CustomIcon item)
        {
            var index = _customIcons.FindIndex(a => a.Id == item.Id);

            if (index >= 0)
            {
                _customIcons[index] = item.Clone();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(CustomIcon item)
        {
            var index = _customIcons.FindIndex(a => a.Id == item.Id);

            if (index >= 0)
            {
                _customIcons.RemoveAt(index);
            }

            return Task.CompletedTask;
        }
    }
}