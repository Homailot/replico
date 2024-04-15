using System.Collections.Generic;
using System.Numerics;

namespace Clustering
{
    public interface IClusterDetector
    {
        public IEnumerable<int> Cluster(Vector2[] points);
        public delegate float DistanceFunction(Vector2 a, Vector2 b);
    }
}