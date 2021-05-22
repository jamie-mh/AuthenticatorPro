// Copyright (C) 2021 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared.Data;

namespace AuthenticatorPro.Test
{
    public class CustomIconComparer : IEqualityComparer<CustomIcon>
    {
        public bool Equals(CustomIcon x, CustomIcon y)
        {
            if(ReferenceEquals(x, y)) return true;
            if(ReferenceEquals(x, null)) return false;
            if(ReferenceEquals(y, null)) return false;
            if(x.GetType() != y.GetType()) return false;

            return x.Id == y.Id && x.Data.SequenceEqual(y.Data);
        }

        public int GetHashCode(CustomIcon obj)
        {
            return HashCode.Combine(obj.Id, obj.Data);
        }
    }
}