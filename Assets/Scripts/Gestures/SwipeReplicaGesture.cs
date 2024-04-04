using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class SwipeReplicaGesture : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly List<Vector2> _fingerStarts = new List<Vector2>();
        
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            
            var fingers = Touch.activeFingers;
            
            if (fingers.Count != gestureConfiguration.swipeFingers)
            {
                return;
            }
            
            foreach (var finger in fingers)
            {
                _fingerStarts.Add(finger.screenPosition);
            }
            _gestureConfiguration.replica.AnimateTo(0);
            _gestureConfiguration.replica.EnableReplica();
        }
        
        public void OnUpdate()
        {
            var fingers = Touch.activeFingers;
            if (fingers.Count != _gestureConfiguration.swipeFingers)
            {
                _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
                // TODO: animate back to 0 with a smooth transition before disabling the replica
                _gestureConfiguration.replica.DisableReplica();
                return;
            }
            
            var fingerDeltaSum = Vector2.zero;
            for (var i = 0; i < fingers.Count; i++)
            {
                var fingerDelta = fingers[i].screenPosition - _fingerStarts[i];
                fingerDeltaSum += fingerDelta;
            }
            
            var fingerDeltaAverage = fingerDeltaSum / fingers.Count;
            var fingerDeltaAverageRelativeToScreen = fingerDeltaAverage / new Vector2(Screen.width, Screen.height);
            
            var fingerDeltaProjected = Vector2.Dot(fingerDeltaAverageRelativeToScreen, _gestureConfiguration.swipeThreshold) / _gestureConfiguration.swipeThreshold.sqrMagnitude;
            _gestureConfiguration.replica.AnimateTo(Mathf.Clamp01(fingerDeltaProjected));
            
            if (!(fingerDeltaProjected > 1f)) return;
            
            Debug.Log("Swipe detected");
            _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _gestureConfiguration));
        } 
    }
}