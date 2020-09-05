using System;
using System.Collections.Generic;
using AuthenticatorPro.Shared.Query;

namespace AuthenticatorPro.WearOS.Data
{
    internal class WearCategoryResponseComparer : IEqualityComparer<WearCategoryResponse>
    {
        public bool Equals(WearCategoryResponse x, WearCategoryResponse y)
        {
            if(ReferenceEquals(x, y)) return true;
            if(ReferenceEquals(x, null)) return false;
            if(ReferenceEquals(y, null)) return false;
            if(x.GetType() != y.GetType()) return false;
            return x.Id == y.Id && x.Name == y.Name;
        }

        public int GetHashCode(WearCategoryResponse obj)
        {
            return HashCode.Combine(obj.Id, obj.Name);
        }
    }
}