// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared.Data;
using AuthenticatorPro.Shared.Data.Backup;

namespace AuthenticatorPro.Test
{
    public class BackupComparer : IEqualityComparer<Backup>
    {
        public bool Equals(Backup x, Backup y)
        {
            if(ReferenceEquals(x, y)) return true;
            if(ReferenceEquals(x, null)) return false;
            if(ReferenceEquals(y, null)) return false;
            if(x.GetType() != y.GetType()) return false;

            return x.Authenticators.SequenceEqual(y.Authenticators, new AuthenticatorComparer()) &&
                   x.Categories.SequenceEqual(y.Categories, new CategoryComparer()) &&
                   x.AuthenticatorCategories.SequenceEqual(y.AuthenticatorCategories, new AuthenticatorCategoryComparer()) &&
                   x.CustomIcons.SequenceEqual(y.CustomIcons, new CustomIconComparer());
        }

        public int GetHashCode(Backup obj)
        {
            return HashCode.Combine(obj.Authenticators, obj.Categories, obj.AuthenticatorCategories, obj.CustomIcons);
        }
    }
}