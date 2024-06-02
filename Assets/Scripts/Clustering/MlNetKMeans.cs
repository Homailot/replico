using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Clustering
{
    public class MlNetKMeans : IClusterDetector
    {
        private readonly int _k;
        private readonly IClusterDetector.DistanceFunction _distanceFunction;
        private readonly MLContext _mlContext;

        private class Point
        {
            [VectorType(2)]
            public float[] features { get; set; }
            
            [KeyType(2)]
            public uint label { get; set; }
        }

        private class Prediction
        {
            public uint PredictedLabel { get; set; }
        }
        
        public MlNetKMeans(int k, IClusterDetector.DistanceFunction distanceFunction)
        {
            _k = k;
            _distanceFunction = distanceFunction;
            _mlContext = new MLContext(seed: 0);
        }
        
        private static IEnumerable<Point> ConvertToPoints(IEnumerable<Vector2> points)
        {
            return points.Select(point => new Point
            {
                features = new[] {point.X, point.Y},
                label = 0
            });
        }
        
        public IEnumerable<int> Cluster(Vector2[] points)
        {
            if (points.Length < _k)
            {
                return Enumerable.Repeat(0, points.Length);
            }
            
            var data = ConvertToPoints(points);
            var dataView = _mlContext.Data.LoadFromEnumerable(data);
            
            var options = new KMeansTrainer.Options
            {
                FeatureColumnName = "features",
                NumberOfClusters = _k,
                InitializationAlgorithm = KMeansTrainer.InitializationAlgorithm.KMeansYinyang,
            };
            var pipeline = _mlContext.Clustering.Trainers.KMeans(options);
            
            var model = pipeline.Fit(dataView);
            var predictions = model.Transform(dataView);
            
            var labels = _mlContext.Data.CreateEnumerable<Prediction>(predictions, reuseRowObject: false)
                .Select(p => (int) p.PredictedLabel);
            
            return labels; 
        }
    }
}