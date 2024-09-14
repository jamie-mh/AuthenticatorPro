// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using Stratum.Core.Entity;

namespace Stratum.Core.Comparer
{
    public class AuthenticatorCategoryComparer : IEqualityComparer<AuthenticatorCategory>
    {
        public bool Equals(AuthenticatorCategory x, AuthenticatorCategory y)
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

            return x.CategoryId == y.CategoryId && x.AuthenticatorSecret == y.AuthenticatorSecret &&
                   x.Ranking == y.Ranking;
        }

        public int GetHashCode(AuthenticatorCategory obj)
        {
            return HashCode.Combine(obj.CategoryId, obj.AuthenticatorSecret, obj.Ranking);
        }
    }
}