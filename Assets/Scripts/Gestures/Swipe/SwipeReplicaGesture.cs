using System.Collections.Generic;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.Swipe
{
    public abstract class SwipeReplicaGesture : IGestureState
    {
        private readonly List<Vector2> _fingerStarts = new();
        private readonly GestureConfiguration _gestureConfiguration;
        private bool _cancelled;
        private float _t;
        
        protected abstract Vector2 swipeThreshold { get; }
        protected abstract void OnSwipeDetected();
        protected abstract void OnSwipeCancelled(float t);
        protected abstract void OnSwipeMoved(float t);

        protected SwipeReplicaGesture(GestureConfiguration gestureConfiguration)
        {
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
        }
        
        public void OnUpdate()
        {
            var fingers = Touch.activeFingers;
            if (_cancelled)
            {
                if (fingers.Count == _gestureConfiguration.swipeFingers)
                {
                    _cancelled = false;
                    _fingerStarts.Clear();

                    foreach (var finger in fingers)
                    {
                        _fingerStarts.Add(finger.screenPosition - swipeThreshold * new Vector2(Screen.width, Screen.height) * _t);
                    }
                }
                else
                {
                    return;
                }
            }
            
            if (fingers.Count != _gestureConfiguration.swipeFingers)
            {
                OnSwipeCancelled(_t);
                _cancelled = true;
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
            
            var fingerDeltaProjected = Vector2.Dot(fingerDeltaAverageRelativeToScreen, swipeThreshold) / swipeThreshold.sqrMagnitude;
            OnSwipeMoved(fingerDeltaProjected);
            _t = fingerDeltaProjected;
            
            if (!(fingerDeltaProjected > 1f)) return;
            
            OnSwipeDetected();
        } 
    }
}