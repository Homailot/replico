using Gestures.ReplicaTransform;
using UnityEngine;

namespace Gestures.Swipe
{
    public class SwipeDownReplicaGesture : SwipeReplicaGesture
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeDownReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration) : base(gestureConfiguration)
        {
            swipeThreshold = -gestureConfiguration.swipeThreshold;
            gestureConfiguration.replicaController.SetEndTransform(gestureConfiguration.replicaController.GetReplica().transform);
            gestureConfiguration.replicaController.AnimateTo(1.0f);
            
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }

        protected override Vector2 swipeThreshold { get; }
        protected override void OnSwipeDetected()
        {
            _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
            _gestureConfiguration.replicaController.ResetTransforms();
        }

        protected override void OnSwipeCancelled(float t)
        {
            if (t > _gestureConfiguration.swipeHalfThreshold)
            {
                _gestureConfiguration.replicaController.RevertAnimation(() =>
                {
                    _gestureConfiguration.replicaController.ResetTransforms();
                    _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
                });
                return;
            }

            _gestureConfiguration.replicaController.CompleteAnimation(() =>
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
            });
        }

        protected override void OnSwipeMoved(float t)
        {
            _gestureConfiguration.replicaController.AnimateTo(1 - Mathf.Clamp01(t));
        }
    }
}