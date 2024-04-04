using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class TransformReplicaState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;

        private readonly Dictionary<Finger, Vector2> _lastFingerPositions =
            new Dictionary<Finger, Vector2>(new FingerEqualityComparer());
        
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
        
        public TransformReplicaState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }

        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == _gestureConfiguration.swipeFingers)
            {
                _gestureDetector.SwitchState(new SwipeDownReplicaGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            var inactiveFingers = _lastFingerPositions.Keys.Where(finger => !finger.isActive).ToList();
            foreach (var inactiveFinger in inactiveFingers)
            {
                _lastFingerPositions.Remove(inactiveFinger);
            }

            if (Touch.activeFingers.Count == 0) {
                return;
            }

            var fingerXDelta = 0.0f;
            var fingerYDelta = 0.0f;
            foreach (var finger in Touch.activeFingers)
            {
                if (!_lastFingerPositions.ContainsKey(finger))
                {
                    _lastFingerPositions.Add(finger, finger.screenPosition);
                    continue;
                }
                
                var lastFingerPosition = _lastFingerPositions[finger];
                fingerXDelta += finger.screenPosition.x - lastFingerPosition.x;
                fingerYDelta += finger.screenPosition.y - lastFingerPosition.y;
                
                _lastFingerPositions[finger] = finger.screenPosition;
            }

            _gestureConfiguration.replica.GetReplica().transform.position += new Vector3(
                fingerXDelta * _gestureConfiguration.translateSpeed / Touch.activeFingers.Count, 
                0.0f,
                fingerYDelta * _gestureConfiguration.translateSpeed / Touch.activeFingers.Count
                );               
        }
    }
}