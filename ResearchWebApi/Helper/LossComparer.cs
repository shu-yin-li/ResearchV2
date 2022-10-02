using System;
using System.Collections.Generic;

namespace ResearchWebApi.Helper
{
    public class LossComparer : IEqualityComparer<double?>
    {
        public LossComparer()
        {
        }

        bool IEqualityComparer<double?>.Equals(double? x, double? y)
        {
            if (x is null) return false;
            if (y is null) return false;
            return x < y;
        }

        int IEqualityComparer<double?>.GetHashCode(double? obj)
        {
            throw new NotImplementedException();
        }
    }
}
