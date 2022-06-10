// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Shared.Entity;

namespace AuthenticatorPro.Shared.Persistence
{
    public interface ICustomIconRepository : IAsyncRepository<CustomIcon, string>
    {
    }
}