using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

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
                _gestureConfiguration.logger.StartTransform();
                _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, new ReplicaTransformer(_gestureConfiguration), _gestureConfiguration));
            }
        }
        
        public void OnEnter()
        {
            var replicaTransform = _gestureConfiguration.replicaController.GetReplica().transform;
            
            if (_gestureConfiguration.replicaController.GetMovementTarget() == null)
            {
                _gestureConfiguration.movementTarget.position = replicaTransform.position;
                _gestureConfiguration.movementTarget.rotation = replicaTransform.rotation;
                _gestureConfiguration.movementTarget.localScale = replicaTransform.localScale;
                _gestureConfiguration.replicaController.SetMovementTarget(_gestureConfiguration.movementTarget);
            }
        }
    }
}