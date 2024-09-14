// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core.Service
{
    public interface ICustomIconService
    {
        public Task AddIfNotExistsAsync(CustomIcon icon);
        public Task<int> AddManyAsync(IEnumerable<CustomIcon> icons);
        public Task<List<CustomIcon>> GetAllAsync();
        public Task CullUnusedAsync();
    }
}