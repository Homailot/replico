using System.Collections.Generic;
using CustomCollections;
using Gestures.HandDetection;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class TransformReplicaState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;

        private Vector2 _lastCenter;
        private float _lastDistance;
        private readonly Dictionary<Finger, Vector2> _lastFingerPositions = new(new FingerEqualityComparer());
        private readonly OrderedSet<Finger> _fingerQueue = new(new FingerEqualityComparer());
        
        private float _timeSinceLastTouch;
        private float _timeSinceHandsDetected;
        private bool _handsMoved;

        private Hands _hands;
        private Vector2 _lastFirstHandCenter;
        private Vector2 _lastSecondHandCenter;
        
        public TransformReplicaState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = new HandDetector(2, _gestureConfiguration.handDistanceThreshold);
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
        
        private bool DetectHandsStill(Hands hands)
        {
            if (hands.IsEmpty() || _handsMoved) return false;
           
            var firstHandCenter = hands.GetFirstHandCenter();
            var secondHandCenter = hands.GetSecondHandCenter();
            
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            var firstHandMoved = Vector2.Distance(firstHandCenter, _lastFirstHandCenter) / screenMax > _gestureConfiguration.handMovementDetectionDistance;
            var secondHandMoved = Vector2.Distance(secondHandCenter, _lastSecondHandCenter) / screenMax > _gestureConfiguration.handMovementDetectionDistance;
            
            if (firstHandMoved || secondHandMoved)
            {
                _timeSinceHandsDetected = 0;
                _lastFirstHandCenter = firstHandCenter;
                _lastSecondHandCenter = secondHandCenter;
                _handsMoved = true;
                Debug.Log("Hands moved");
                return false;
            }
            _timeSinceHandsDetected += Time.deltaTime;

            return _timeSinceHandsDetected > _gestureConfiguration.handMovementDetectionTime;
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
                _timeSinceHandsDetected = 0;
                _handsMoved = false;
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

            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands, _fingerQueue);
            if (!hands.IsEmpty())
            {
                if (_hands.IsEmpty())
                {
                     _timeSinceHandsDetected = 0;
                     _handsMoved = false;
                     _lastFirstHandCenter = hands.GetFirstHandCenter();
                     _lastSecondHandCenter = hands.GetSecondHandCenter();
                }

                var gestureDetected = DetectHandsStill(hands);

                if (gestureDetected)
                {
                    Debug.Log("Hands still detected");
                }
            }
            _hands = hands;
            
            Debug.Log(_hands.IsEmpty()
                ? "No hands detected"
                : $"Hands detected: {_hands.firstHand.Count} {_hands.secondHand.Count}");

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