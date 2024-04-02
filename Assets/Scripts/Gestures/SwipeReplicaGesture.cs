using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class SwipeReplicaGesture : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly Vector2 _finger1Start;
        private readonly Vector2 _finger2Start; 
        
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            
            var fingers = Touch.activeFingers;
            
            if (fingers.Count != 2)
            {
                return;
            }
            
            _finger1Start = fingers[0].screenPosition;
            _finger2Start = fingers[1].screenPosition;
        }
        
        public void OnUpdate()
        {
            var fingers = Touch.activeFingers;
            if (fingers.Count != 2)
            {
                _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            var finger1 = fingers[0];
            var finger2 = fingers[1];
            
            var finger1Delta = finger1.screenPosition - _finger1Start;
            var finger2Delta = finger2.screenPosition - _finger2Start;
            
            var fingerDeltaAverage = (finger1Delta + finger2Delta) / 2;
            var fingerDeltaAverageRelativeToScreen = fingerDeltaAverage / new Vector2(Screen.width, Screen.height);
            
            Debug.Log($"Finger delta average: {fingerDeltaAverage}");
            Debug.Log($"Finger delta average relative to screen: {fingerDeltaAverageRelativeToScreen}");
            
            Debug.Log($"Swipe threshold: {_gestureConfiguration.swipeThreshold}");
            var fingerDeltaProjected = Vector2.Dot(fingerDeltaAverageRelativeToScreen, _gestureConfiguration.swipeThreshold) / _gestureConfiguration.swipeThreshold.sqrMagnitude;

            Debug.Log($"Finger delta projected: {fingerDeltaProjected}");
            
            if (!(fingerDeltaProjected > 1f)) return;
            
            Debug.Log("Swipe detected");
            _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
        } 
    }
}