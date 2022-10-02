﻿using System;
using System.Collections.Generic;

namespace ResearchWebApi.Helper
{
    public class GainComparer : IEqualityComparer<double?>
    {
        public GainComparer()
        {
        }

        bool IEqualityComparer<double?>.Equals(double? x, double? y)
        {
            if (x is null) return false;
            if (y is null) return false;
            return x >= y;
        }

        int IEqualityComparer<double?>.GetHashCode(double? obj)
        {
            throw new NotImplementedException();
        }
    }
}
