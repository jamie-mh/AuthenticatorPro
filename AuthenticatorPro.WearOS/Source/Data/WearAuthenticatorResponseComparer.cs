using System;
using System.Collections.Generic;
using System.Linq;
using AuthenticatorPro.Shared.Query;

namespace AuthenticatorPro.WearOS.Data
{
    internal class WearAuthenticatorResponseComparer : IEqualityComparer<WearAuthenticatorResponse>
    {
        public bool Equals(WearAuthenticatorResponse x, WearAuthenticatorResponse y)
        {
            if(ReferenceEquals(x, y)) return true;
            if(ReferenceEquals(x, null)) return false;
            if(ReferenceEquals(y, null)) return false;
            if(x.GetType() != y.GetType()) return false;

            var differentCategories =
                x.CategoryIds.Except(y.CategoryIds).Any() || y.CategoryIds.Except(x.CategoryIds).Any();

            var isEqual = !differentCategories && x.Secret == y.Secret && x.Icon == y.Icon && x.Issuer == y.Issuer &&
                x.Username == y.Username && x.Period == y.Period && x.Digits == y.Digits && x.Algorithm == y.Algorithm;

            return isEqual;
        }

        public int GetHashCode(WearAuthenticatorResponse obj)
        {
            return HashCode.Combine(obj.Secret, obj.Icon, obj.Issuer, obj.Username, obj.Period, obj.Digits, (int) obj.Algorithm, obj.CategoryIds);
        }
    }
}