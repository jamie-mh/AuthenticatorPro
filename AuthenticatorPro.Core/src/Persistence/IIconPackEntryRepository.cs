// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Persistence
{
    public interface IIconPackEntryRepository : IAsyncRepository<IconPackEntry, ValueTuple<string, string>>
    {
        public Task<List<IconPackEntry>> GetAllForPackAsync(IconPack pack);
        public Task DeleteAllForPackAsync(IconPack pack);
    }
}