using System.Collections.Generic;
using System.Linq;
using Clustering;
using CustomCollections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Utils;

namespace Gestures.HandDetection
{
    public class HandDetector
    {
        private readonly IClusterDetector _kMeans;
        private readonly float _handDistanceThreshold;
        private readonly OrderedSet<Finger> _fingerQueue = new(new FingerEqualityComparer());
        
        public HandDetector(int k, float handDistanceThreshold)
        {
            _kMeans = new MlNetKMeans(k, System.Numerics.Vector2.Distance);
            _handDistanceThreshold = handDistanceThreshold;
        }
         
        private Hands UpdateHands(ReadOnlyArray<Finger> fingers, IReadOnlyList<int> clusters, Hands previousHands)
        {
            var firstHand = new HashSet<Finger>(new FingerEqualityComparer());
            firstHand.UnionWith(previousHands.firstHand.Where(finger => finger.isActive));
            var secondHand = new HashSet<Finger>(new FingerEqualityComparer());
            secondHand.UnionWith(previousHands.secondHand.Where(finger => finger.isActive));
            
            var handFingers = new HashSet<Finger>(new FingerEqualityComparer());
            handFingers.UnionWith(firstHand);
            handFingers.UnionWith(secondHand);
            
            Debug.Log("identified clusters");
            for (var i = 0; i < fingers.Count; i++) 
            {
                Debug.Log("Finger: " + fingers[i].screenPosition + " Cluster: " + clusters[i]); 
            }
            
            // identify which cluster corresponds to which hand
            var firstHandClusterCount = new Dictionary<int, int>();
            for (var i = 0; i < fingers.Count; i++)
            {
                var finger = fingers[i];
                var cluster = clusters[i];
                var quantity = firstHand.Contains(finger) ? 1 : -1;
                
                if (!firstHandClusterCount.TryAdd(cluster, quantity))
                {
                    firstHandClusterCount[cluster] += quantity;
                }
            }

            var firstCluster = firstHandClusterCount.Count == 0 ? -1 : firstHandClusterCount.First().Key;
            foreach (var (cluster, count) in firstHandClusterCount)
            {
                if (count > firstHandClusterCount[firstCluster])
                {
                    firstCluster = cluster;
                } 
            }
            
            for (var i = 0; i < fingers.Count; i++)
            {
                var finger = fingers[i];
                var cluster = clusters[i];
                
                // only consider fingers that aren't already in a hand
                if (handFingers.Contains(finger)) continue;
                
                if (cluster == firstCluster)
                {
                    firstHand.Add(finger);
                }
                else
                {
                    secondHand.Add(finger);
                }
            }
            
            return new Hands(firstHand, secondHand);
        }

        public Hands DetectHands(ReadOnlyArray<Finger> fingers, Hands previousHands)
        {
            while (_fingerQueue.Count != 0 && (!_fingerQueue.GetFirst()?.isActive ?? false))
            {
                _fingerQueue.RemoveFirst();
            }
            
            foreach (var finger in fingers)
            {
                _fingerQueue.Add(finger);
            }

            var points = fingers.Select(finger => new System.Numerics.Vector2(finger.screenPosition.x,
                finger.screenPosition.y));
            
            var clusters = _kMeans.Cluster(points.ToArray());
            var clustersArray = clusters as int[] ?? clusters.ToArray();

            if (!previousHands.IsEmpty())
            {
                return UpdateHands(fingers, clustersArray, previousHands);
            }

            var firstCluster = clustersArray[0];
            // Determine which cluster is the first one
            for (var i = 0; i < fingers.Count; i++)
            {
                var firstFinger = _fingerQueue.GetFirst();
                if (firstFinger != fingers[i]) continue;
                
                firstCluster = clustersArray[i];
                break;
            }
            Debug.Log("First first cluster: " + firstCluster);
            
            // Calculate centroids
            var firstHand = new HashSet<Finger>(new FingerEqualityComparer());
            var secondHand = new HashSet<Finger>(new FingerEqualityComparer());
            
            var centroid1 = new Vector2(0, 0);
            var centroid2 = new Vector2(0, 0);
            for (var i = 0; i < fingers.Count; i++)
            {
                var finger = fingers[i];
                var cluster = clustersArray[i];
                
                if (cluster == firstCluster)
                {
                    centroid1 += finger.screenPosition;
                    firstHand.Add(finger);
                }
                else
                {
                    centroid2 += finger.screenPosition;
                    secondHand.Add(finger);
                }
            }

            if (firstHand.Count == 0 || secondHand.Count == 0)
            {
                return Hands.none;
            }
            
            centroid1 /= firstHand.Count;
            centroid2 /= secondHand.Count;
            
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            var centroidDistance = Vector2.Distance(centroid1, centroid2) / screenMax;
            
            return centroidDistance < _handDistanceThreshold ? Hands.none : new Hands(firstHand, secondHand);
        }
    }
}