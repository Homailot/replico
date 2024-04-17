using System;
using System.Collections.Generic;
using System.Linq;
using Clustering;
using CustomCollections;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class TransformReplicaState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly IClusterDetector _kMeans;

        private Vector2 _lastCenter;
        private float _lastDistance;
        private readonly Dictionary<Finger, Vector2> _lastFingerPositions = new(new FingerEqualityComparer());
        private readonly OrderedSet<Finger> _fingerQueue = new(new FingerEqualityComparer());
        
        private float _timeSinceLastTouch;
        private float _timeSinceHandsDetected;

        private Hands _hands;
        
        public TransformReplicaState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _kMeans = new MlNetKMeans(2, System.Numerics.Vector2.Distance);
            _hands = Hands.none;
        }
           
        private static Vector2 CalculateCenter(IReadOnlyList<Finger> touches)
        {
            var tMax = touches[0].screenPosition;
            var tMin = tMax;

            for (var i = 1; i < touches.Count; i++)
            {
                var tPos = touches[i].screenPosition;
                tMax = new Vector2(Mathf.Max(tMax.x, tPos.x), Mathf.Max(tMax.y, tPos.y));
                tMin = new Vector2(Mathf.Min(tMin.x, tPos.x), Mathf.Min(tMin.y, tPos.y));
            }

            return (tMin + tMax) / 2.0f;
        }

        private static float CalculateAverageDistance(IReadOnlyCollection<Finger> touches, Vector2 center)
        {
            float avgDistance = 0;
            foreach (var finger in touches)
            {
                avgDistance += (center - finger.screenPosition).magnitude;
            }
            avgDistance /= touches.Count;

            return avgDistance;
        }

        private float CalculateAverageRotation(IReadOnlyCollection<Finger> touches, Vector2 center, Vector2 lastCenter, int lastTouchCount)
        {
            float avgRotation = 0;
            if (lastTouchCount != touches.Count || touches.Count <= 1) return avgRotation;
            
            foreach (var finger in touches)
            {
                var oldDir = _lastFingerPositions[finger] - lastCenter;
                var newDir = finger.screenPosition - center;
                var angle = Vector2.Angle(oldDir, newDir);
                
                if (Vector3.Cross(oldDir, newDir).z < 0) angle = -angle;
                avgRotation += angle;
            }
            avgRotation /= touches.Count;

            return avgRotation;
        }
        
        private Hands UpdateHands(ReadOnlyArray<Finger> fingers, IReadOnlyList<int> clusters)
        {
            var firstHand = new HashSet<Finger>(new FingerEqualityComparer());
            firstHand.UnionWith(_hands.firstHand.Where(finger => finger.isActive));
            var secondHand = new HashSet<Finger>(new FingerEqualityComparer());
            secondHand.UnionWith(_hands.secondHand.Where(finger => finger.isActive));
            
            var handFingers = new HashSet<Finger>(new FingerEqualityComparer());
            handFingers.UnionWith(firstHand);
            handFingers.UnionWith(secondHand);
            
            // identify which cluster corresponds to which hand
            var firstHandCluster1Count = 0;
            var firstHandCluster2Count = 0;
            for (var i = 0; i < fingers.Count; i++)
            {
                var finger = fingers[i];
                var cluster = clusters[i];
                if (!firstHand.Contains(finger)) continue;
                if (cluster == 0)
                {
                    firstHandCluster1Count++;
                }
                else
                {
                    firstHandCluster2Count++;
                }
            }
            
            var firstCluster = firstHandCluster1Count > firstHandCluster2Count ? 0 : 1;
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

        private Hands DetectHands(ReadOnlyArray<Finger> fingers)
        {
            var points = fingers.Select(finger => new System.Numerics.Vector2(finger.screenPosition.x,
                finger.screenPosition.y));
            
            var clusters = _kMeans.Cluster(points.ToArray());
            var clustersArray = clusters as int[] ?? clusters.ToArray();

            if (!_hands.IsEmpty())
            {
                return UpdateHands(fingers, clustersArray);
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
            
            return centroidDistance < _gestureConfiguration.handDistanceThreshold ? Hands.none : new Hands(firstHand, secondHand);
        }

        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == _gestureConfiguration.swipeFingers && _timeSinceLastTouch <= _gestureConfiguration.swipeGestureTimeDetection)
            {
                _gestureDetector.SwitchState(new SwipeDownReplicaGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            if (Touch.activeFingers.Count == 0) {
                _lastFingerPositions.Clear();
                _fingerQueue.Clear();
                _hands = Hands.none;
                _lastCenter = Vector2.zero;
                _lastDistance = 0;
                _timeSinceLastTouch = 0;
                return;
            }
            
            if (_lastFingerPositions.Count == 0)
            {
                foreach (var finger in Touch.activeFingers)
                {
                    _lastFingerPositions.Add(finger, finger.screenPosition);
                }
                _lastCenter = CalculateCenter(Touch.activeFingers);
                _lastDistance = CalculateAverageDistance(Touch.activeFingers, _lastCenter);
                return;
            }
            _timeSinceLastTouch += Time.deltaTime;

            while (_fingerQueue.Count != 0 && (!_fingerQueue.GetFirst()?.isActive ?? false))
            {
                _fingerQueue.RemoveFirst();
            }
            
            foreach (var finger in Touch.activeFingers)
            {
                _fingerQueue.Add(finger);
            }

            var hands = DetectHands(Touch.activeFingers);
            Debug.Log(hands.IsEmpty()
                ? "No hands detected"
                : $"Hands detected: {hands.firstHand.Count} {hands.secondHand.Count}");
            _hands = hands;

            var touchCount = Touch.activeFingers.Count;
            var touchCenter = CalculateCenter(Touch.activeFingers);
            var touchDistance = CalculateAverageDistance(Touch.activeFingers, touchCenter);
            var touchRotation = CalculateAverageRotation(Touch.activeFingers, touchCenter, _lastCenter, _lastFingerPositions.Count);
            
            if (_lastFingerPositions.Count == touchCount)
            {
                if (touchCount > 1)
                {
                    var scale = touchDistance / _lastDistance;
                    scale = Mathf.Pow(scale, _gestureConfiguration.scaleSpeed);
                    _gestureConfiguration.movementTarget.localScale *= scale;
                }

                var touchPlaneFingerPosition = _gestureConfiguration.touchToPosition.GetTouchPosition(touchCenter);
                _gestureConfiguration.movementTarget.RotateAround(touchPlaneFingerPosition, Vector3.up, -touchRotation);
                _gestureConfiguration.movementTarget.position += new Vector3(
                                     (touchCenter.x - _lastCenter.x) * _gestureConfiguration.translateSpeed, 
                                     0.0f,
                                     (touchCenter.y - _lastCenter.y) * _gestureConfiguration.translateSpeed
                                     );               
            }
           
            _lastFingerPositions.Clear();
            foreach (var finger in Touch.activeFingers)
            {
                _lastFingerPositions.Add(finger, finger.screenPosition);
            }
            _lastCenter = touchCenter;
            _lastDistance = touchDistance;
        }

        public void OnEnter()
        {
            var replicaTransform = _gestureConfiguration.replicaController.GetReplica().transform;
            
            _gestureConfiguration.movementTarget.position = replicaTransform.position;
            _gestureConfiguration.movementTarget.rotation = replicaTransform.rotation;
            _gestureConfiguration.movementTarget.localScale = replicaTransform.localScale;
            _gestureConfiguration.replicaController.SetMovementTarget(_gestureConfiguration.movementTarget);
        }

        public void OnExit()
        {
            _gestureConfiguration.replicaController.SetMovementTarget(null);
        }
    }
}