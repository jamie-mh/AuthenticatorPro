// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Stratum.Core.Entity;

namespace Stratum.Core.Comparer
{
    public class CustomIconComparer : IEqualityComparer<CustomIcon>
    {
        public bool Equals(CustomIcon x, CustomIcon y)
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

            // No need to compare the data as the id is a hash
            return x.Id == y.Id;
        }

        public int GetHashCode(CustomIcon obj)
        {
            return HashCode.Combine(obj.Id);
        }
    }
}