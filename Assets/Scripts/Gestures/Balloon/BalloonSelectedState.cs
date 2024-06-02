using System.Linq;
using Gestures.ReplicaTransform;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures.Balloon
{
    public class BalloonSelectedState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public BalloonSelectedState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
        }
        
        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == 0)
            {
                _gestureDetector.OnGestureExit();
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
            }
        }
    }
}