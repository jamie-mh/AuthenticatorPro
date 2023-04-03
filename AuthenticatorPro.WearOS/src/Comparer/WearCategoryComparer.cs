// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using AuthenticatorPro.Droid.Shared.Wear;
using System;
using System.Collections.Generic;

namespace AuthenticatorPro.WearOS.Comparer
{
    internal class WearCategoryComparer : IEqualityComparer<WearCategory>
    {
        public bool Equals(WearCategory x, WearCategory y)
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

            return x.Id == y.Id && x.Name == y.Name && x.Ranking == y.Ranking;
        }

        public int GetHashCode(WearCategory obj)
        {
            return HashCode.Combine(obj.Id, obj.Name, obj.Ranking);
        }
    }
}