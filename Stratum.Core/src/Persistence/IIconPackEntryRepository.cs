// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stratum.Core.Entity;

namespace Stratum.Core.Persistence
{
    public interface IIconPackEntryRepository : IAsyncRepository<IconPackEntry, ValueTuple<string, string>>
    {
        public Task CreateManyAsync(List<IconPackEntry> items);
        public Task<List<IconPackEntry>> GetAllForPackAsync(IconPack pack);
        public Task DeleteAllForPackAsync(IconPack pack);
    }
}