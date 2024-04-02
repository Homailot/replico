using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class InitialGesture : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public InitialGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }
        
        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == 2)
            {
                _gestureDetector.SwitchState(new SwipeReplicaGesture(_gestureDetector, _gestureConfiguration)); 
            }
        }
    }
}