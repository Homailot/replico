using System.Collections.Generic;
using System.Numerics;

namespace Clustering
{
    public class DBScan : IClusterDetector
    {
        private readonly float _eps;
        private readonly int _minPts;
        private readonly IClusterDetector.DistanceFunction _distanceFunction;
        
        public DBScan(float eps, int minPts, IClusterDetector.DistanceFunction distanceFunction)
        {
            _eps = eps;
            _minPts = minPts;
            _distanceFunction = distanceFunction;
        }
        
        public IEnumerable<int> Cluster(Vector2[] points)
        {
            var labels = new int[points.Length];
            var clusterId = 0;

            for (var i = 0; i < points.Length; i++)
            {
                if (labels[i] != 0)
                {
                    continue;
                }
                var point = points[i];

                var neighbors = GetNeighbors(point, points);
                if (neighbors.Count < _minPts)
                {
                    labels[i] = -1;
                    continue;
                }

                clusterId++;
                labels[i] = clusterId;

                for (var j = 0; j < neighbors.Count; j++)
                {
                    var neighbor = neighbors[j];
                    if (neighbor == i)
                    {
                        continue;
                    }
                    if (labels[neighbor] == -1)
                    {
                        labels[neighbor] = clusterId;
                    }
                    if (labels[neighbor] != 0)
                    {
                        continue;
                    }
                    labels[neighbor] = clusterId;
                    
                    var neighborNeighbors = GetNeighbors(points[neighbor], points);
                    if (neighborNeighbors.Count >= _minPts)
                    {
                        neighbors.AddRange(neighborNeighbors);
                    } 
                }
            }
            
            return labels;
        }
        
        private List<int> GetNeighbors(Vector2 point, IReadOnlyList<Vector2> points)
        {
            var neighbors = new List<int>();
            for (var i = 0; i < points.Count; i++)
            {
                if (_distanceFunction(point, points[i]) < _eps)
                {
                    neighbors.Add(i);
                }
            }

            return neighbors;
        }
    }
}