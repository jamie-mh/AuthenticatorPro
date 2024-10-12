// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Stratum.Droid.Shared.Wear;

namespace Stratum.WearOS.Comparer
{
    public class WearAuthenticatorCategoryComparer : IEqualityComparer<WearAuthenticatorCategory>
    {
        public bool Equals(WearAuthenticatorCategory x, WearAuthenticatorCategory y)
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

            return x.CategoryId == y.CategoryId && x.Ranking == y.Ranking;
        }

        public int GetHashCode(WearAuthenticatorCategory item)
        {
            return HashCode.Combine(item.CategoryId, item.Ranking);
        }
    }
}