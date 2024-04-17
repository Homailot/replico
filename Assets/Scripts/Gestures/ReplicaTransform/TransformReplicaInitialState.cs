using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures.ReplicaTransform
{
    public class TransformReplicaInitialState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public TransformReplicaInitialState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }
        
        public void OnUpdate()
        {
            if (Touch.activeFingers.Count > 0)
            {
                _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _gestureConfiguration));
            }
        }
    }
}