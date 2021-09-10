// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthenticatorPro.Shared.Service
{
    public interface ICustomIconService
    {
        public Task AddIfNotExists(CustomIcon icon);
        public Task<int> AddManyAsync(IEnumerable<CustomIcon> icons);
        public Task CullUnused();
    }
}