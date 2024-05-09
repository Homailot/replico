using System;
using System.Collections.Generic;
using Gestures.Balloon;

namespace Utils
{
    public class BalloonTempEqualityComparer : IEqualityComparer<BalloonPointTempId>
    {
        public bool Equals(BalloonPointTempId x, BalloonPointTempId y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.playerId == y.playerId && x.position.Equals(y.position);
        }

        public int GetHashCode(BalloonPointTempId obj)
        {
            return HashCode.Combine(obj.playerId, obj.position);
        }
    }
}