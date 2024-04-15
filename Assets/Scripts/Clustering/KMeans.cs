using System.Collections.Generic;
using System.Numerics;

namespace Clustering
{
    public class KMeans : IClusterDetector
    {
        private readonly int _k;
        private readonly IClusterDetector.DistanceFunction _distanceFunction;
        
        public KMeans(int k, IClusterDetector.DistanceFunction distanceFunction)
        {
            _k = k;
            _distanceFunction = distanceFunction;
        }
        
        public IEnumerable<int> Cluster(Vector2[] points)
        {
            var labels = new int[points.Length];
            
            var centroidLength = _k > points.Length ? points.Length : _k;
            var initialCentroids = new Vector2[centroidLength];
            for (var i = 0; i < centroidLength; i++)
            {
                initialCentroids[i] = points[i];
            }

            var changed = true;
            while (changed)
            {
                changed = false;
                
                for (var i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    var minDistance = float.MaxValue;
                    var minCentroid = -1;
                    for (var j = 0; j < initialCentroids.Length; j++)
                    {
                        var centroid = initialCentroids[j];
                        var distance = _distanceFunction(point, centroid);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            minCentroid = j;
                        }
                    }
                    if (labels[i] != minCentroid)
                    {
                        labels[i] = minCentroid;
                        changed = true;
                    }
                }
                
                for (var i = 0; i < initialCentroids.Length; i++)
                {
                    var centroid = new Vector2(0, 0);
                    var count = 0;
                    for (var j = 0; j < points.Length; j++)
                    {
                        if (labels[j] == i)
                        {
                            centroid += points[j];
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        initialCentroids[i] = centroid / count;
                    }
                }
            }
            
            return labels;
        }
    }
}