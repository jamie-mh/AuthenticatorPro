// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using AuthenticatorPro.Shared.Persistence;
using AuthenticatorPro.Shared.Persistence.Exception;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticatorPro.Test
{
    public class InMemoryCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = new List<Category>();

        public Task CreateAsync(Category item)
        {
            if (_categories.Any(a => a.Id == item.Id))
            {
                throw new EntityDuplicateException();
            }

            _categories.Add(item.Clone());
            return Task.CompletedTask;
        }

        public Task<Category> GetAsync(string id)
        {
            return Task.FromResult(_categories.SingleOrDefault(a => a.Id == id));
        }

        public Task<List<Category>> GetAllAsync()
        {
            return Task.FromResult(_categories);
        }

        public Task UpdateAsync(Category item)
        {
            var index = _categories.FindIndex(c => c.Id == item.Id);

            if (index >= 0)
            {
                _categories[index] = item.Clone();
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Category item)
        {
            var index = _categories.FindIndex(a => a.Id == item.Id);

            if (index >= 0)
            {
                _categories.RemoveAt(index);
            }

            return Task.CompletedTask;
        }
    }
}