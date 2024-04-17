using Gestures.ReplicaTransform;
using UnityEngine;

namespace Gestures.Swipe
{
    public class SwipeUpReplicaGesture : SwipeReplicaGesture
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeUpReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration) : base(gestureConfiguration)
        {
            swipeThreshold = gestureConfiguration.swipeThreshold;
            gestureConfiguration.replicaController.AnimateTo(0.0f);
            
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }

        protected override Vector2 swipeThreshold { get; }
        protected override void OnSwipeDetected()
        {
            _gestureConfiguration.replicaController.ResetTransforms();
            _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
        }

        protected override void OnSwipeCancelled(float t)
        {
            if (t > _gestureConfiguration.swipeHalfThreshold)
            {
                _gestureConfiguration.replicaController.CompleteAnimation(() =>
                {
                    _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                });
                return;
            }
            
            _gestureConfiguration.replicaController.RevertAnimation(() =>
            {
                _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
            });
        }

        protected override void OnSwipeMoved(float t)
        {
            _gestureConfiguration.replicaController.AnimateTo(Mathf.Clamp01(t));
        }
    }
}