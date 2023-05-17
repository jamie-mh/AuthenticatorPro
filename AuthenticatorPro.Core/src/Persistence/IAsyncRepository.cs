// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Persistence
{
    public interface IAsyncRepository<T, in TU> where T : new()
    {
        public Task CreateAsync(T item);
        public Task<T> GetAsync(TU id);
        public Task<List<T>> GetAllAsync();
        public Task UpdateAsync(T item);
        public Task DeleteAsync(T item);
    }
}