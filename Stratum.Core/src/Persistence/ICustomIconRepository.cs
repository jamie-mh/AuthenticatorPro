// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using Stratum.Core.Entity;

namespace Stratum.Core.Persistence
{
    public interface ICustomIconRepository : IAsyncRepository<CustomIcon, string>;
}