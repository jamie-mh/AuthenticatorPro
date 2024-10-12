// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Stratum.Core.Entity;

namespace Stratum.Core.Comparer
{
    public class CategoryComparer : IEqualityComparer<Category>
    {
        public bool Equals(Category x, Category y)
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

        public int GetHashCode(Category obj)
        {
            return HashCode.Combine(obj.Id, obj.Name, obj.Ranking);
        }
    }
}