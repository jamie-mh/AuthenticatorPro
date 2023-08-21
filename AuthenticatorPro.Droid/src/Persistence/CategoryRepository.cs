// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Core.Entity;
using AuthenticatorPro.Core.Persistence;

namespace AuthenticatorPro.Droid.Persistence
{
    internal class CategoryRepository : AsyncRepository<Category, string>, ICategoryRepository
    {
        public CategoryRepository(Database database) : base(database)
        {
        }
    }
}