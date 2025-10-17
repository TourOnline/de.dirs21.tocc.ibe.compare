using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TOCC.IBE.Compare
{
    internal class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        public int GetHashCode(object? obj)
        {
            return obj == null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }
    }
}
