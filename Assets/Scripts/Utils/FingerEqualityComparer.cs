using System.Collections.Generic;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Utils
{
    public class FingerEqualityComparer : IEqualityComparer<Finger>
    {
        public bool Equals(Finger x, Finger y)
        {
            if (x == null || y == null)
            {
                return false;
            }
                
            return x.index == y.index;
        }

        public int GetHashCode(Finger obj)
        {
            return obj.index;
        }
    }
}