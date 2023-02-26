// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;

namespace AuthenticatorPro.Core.Persistence
{
    public interface ICategoryRepository : IAsyncRepository<Category, string>
    {
    }
}