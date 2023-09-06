// Copyright (C) 2023 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;

namespace AuthenticatorPro.Droid.Persistence
{
    public class IconPackRepository : AsyncRepository<IconPack, string>, IIconPackRepository
    {
        public IconPackRepository(Database database) : base(database)
        {
        }
    }
}