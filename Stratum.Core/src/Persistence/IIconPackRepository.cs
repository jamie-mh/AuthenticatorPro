// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Entity;

namespace Stratum.Core.Persistence
{
    public interface IIconPackRepository : IAsyncRepository<IconPack, string>;
}