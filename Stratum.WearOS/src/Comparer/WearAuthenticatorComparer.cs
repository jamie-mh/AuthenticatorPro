// Copyright (C) 2022 jmh
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.Collections.Generic;
using System.Linq;
using Stratum.Droid.Shared.Wear;

namespace Stratum.WearOS.Comparer
{
    public class WearAuthenticatorComparer : IEqualityComparer<WearAuthenticator>
    {
        public bool Equals(WearAuthenticator x, WearAuthenticator y)
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

            if (x.Categories == null && y.Categories != null)
            {
                return false;
            }

            if (x.Categories != null && y.Categories == null)
            {
                return false;
            }

            var differentCategories = false;

            if (x.Categories != null && y.Categories != null)
            {
                var categoryComparer = new WearAuthenticatorCategoryComparer();

                differentCategories =
                    x.Categories.Except(y.Categories, categoryComparer).Any() ||
                    y.Categories.Except(x.Categories, categoryComparer).Any();
            }

            var isEqual = !differentCategories && x.Type == y.Type && x.Secret == y.Secret && x.Icon == y.Icon &&
                          x.Issuer == y.Issuer && x.Username == y.Username && x.Period == y.Period &&
                          x.Digits == y.Digits && x.Algorithm == y.Algorithm && x.Pin == y.Pin &&
                          x.Ranking == y.Ranking && x.CopyCount == y.CopyCount;

            return isEqual;
        }

        public int GetHashCode(WearAuthenticator obj)
        {
            var hash = new HashCode();
            hash.Add(obj.Type);
            hash.Add(obj.Secret);
            hash.Add(obj.Pin);
            hash.Add(obj.Icon);
            hash.Add(obj.Issuer);
            hash.Add(obj.Username);
            hash.Add(obj.Period);
            hash.Add(obj.Digits);
            hash.Add(obj.Algorithm);
            hash.Add(obj.Categories);
            hash.Add(obj.Ranking);
            hash.Add(obj.CopyCount);

            return hash.ToHashCode();
        }
    }
}