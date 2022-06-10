// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Persistence
{
    public interface IAsyncRepository<T, in TU> where T : class
    {
        public Task CreateAsync(T item);
        public Task<T> GetAsync(TU id);
        public Task<List<T>> GetAllAsync();
        public Task UpdateAsync(T item);
        public Task DeleteAsync(T item);
    }
}