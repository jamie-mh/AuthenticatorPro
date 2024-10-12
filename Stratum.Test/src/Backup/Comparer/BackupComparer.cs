// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Stratum.Core.Comparer;

namespace Stratum.Test.Backup.Comparer
{
    public class BackupComparer : IEqualityComparer<Stratum.Core.Backup.Backup>
    {
        public bool Equals(Stratum.Core.Backup.Backup x, Stratum.Core.Backup.Backup y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (ReferenceEquals(x, null))
            {
                return false;
            }

            if (ReferenceEquals(y, null))
            {
                return false;
            }

            if (x.GetType() != y.GetType())
            {
                return false;
            }

            return x.Authenticators.SequenceEqual(y.Authenticators, new AuthenticatorComparer()) &&
                   x.Categories.SequenceEqual(y.Categories, new CategoryComparer()) &&
                   x.AuthenticatorCategories.SequenceEqual(y.AuthenticatorCategories,
                       new AuthenticatorCategoryComparer()) &&
                   x.CustomIcons.SequenceEqual(y.CustomIcons, new CustomIconComparer());
        }

        public int GetHashCode(Stratum.Core.Backup.Backup obj)
        {
            return HashCode.Combine(obj.Authenticators, obj.Categories, obj.AuthenticatorCategories, obj.CustomIcons);
        }
    }
}