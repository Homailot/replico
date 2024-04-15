using System;
using System.Collections.Generic;
using System.Linq;
using Clustering;
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
        private readonly DBScan _dbscan;
        private readonly KMeans _kMeans;

        private Vector2 _lastCenter;
        private float _lastDistance;
        private readonly Dictionary<Finger, Vector2> _lastFingerPositions = new(new FingerEqualityComparer());
        
        private float _timeSinceLastTouch;
        
        private class FingerEqualityComparer : IEqualityComparer<Finger>
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
        
        public TransformReplicaState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _dbscan = new DBScan(_gestureConfiguration.clusterEps, gestureConfiguration.clusterMinPts, System.Numerics.Vector2.Distance);
            _kMeans = new KMeans(2, System.Numerics.Vector2.Distance);
        }

        private void ToClusters(ReadOnlyArray<Finger> fingers)
        {
            var points = fingers.Select(finger => new System.Numerics.Vector2(finger.screenPosition.x / Screen.width,
                finger.screenPosition.y / Screen.height));
            
            var clusters = _dbscan.Cluster(points.ToArray());
            
            Debug.Log("Clusters: " + string.Join(", ", clusters.Select(cluster => cluster.ToString()).ToArray()));
            
            var cluster1 = _kMeans.Cluster(points.ToArray());
            
            Debug.Log("Cluster 1: " + string.Join(", ", cluster1.Select(cluster => cluster.ToString()).ToArray()));
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
                _lastCenter = Vector2.zero;
                _lastDistance = 0;
                _timeSinceLastTouch = 0;
                return;
            }
            ToClusters(Touch.activeFingers);
            
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

            var touchCount = Touch.activeFingers.Count;
            var touchCenter = CalculateCenter(Touch.activeFingers);
            var touchDistance = CalculateAverageDistance(Touch.activeFingers, touchCenter);
            var touchRotation = CalculateAverageRotation(Touch.activeFingers, touchCenter, _lastCenter, _lastFingerPositions.Count);
            
            if (_lastFingerPositions.Count == touchCount)
            {
                if (touchCount > 1)
                {
                    var scale = touchDistance / _lastDistance;
                    // apply scale speed
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