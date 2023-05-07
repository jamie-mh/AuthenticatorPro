// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Core.Persistence
{
    public interface IIconPackEntryRepository : IAsyncRepository<IconPackEntry, ValueTuple<string, string>>
    {
        public Task<List<IconPackEntry>> GetAllForPackAsync(IconPack pack);
        public Task DeleteAllForPackAsync(IconPack pack);
    }
}