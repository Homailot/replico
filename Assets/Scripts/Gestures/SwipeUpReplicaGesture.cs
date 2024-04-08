using UnityEngine;

namespace Gestures
{
    public class SwipeUpReplicaGesture : SwipeReplicaGesture
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeUpReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration) : base(gestureConfiguration)
        {
            swipeThreshold = gestureConfiguration.swipeThreshold;
            gestureConfiguration.replica.AnimateTo(0.0f);
            
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }

        protected override Vector2 swipeThreshold { get; }
        protected override void OnSwipeDetected()
        {
            _gestureConfiguration.replica.ResetTransforms();
            _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _gestureConfiguration));
        }

        protected override void OnSwipeCancelled(float t)
        {
            if (t > _gestureConfiguration.swipeHalfThreshold)
            {
                _gestureConfiguration.replica.CompleteAnimation(() =>
                {
                    _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _gestureConfiguration));
                });
                return;
            }
            
            _gestureConfiguration.replica.RevertAnimation(() =>
            {
                _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
            });
        }

        protected override void OnSwipeMoved(float t)
        {
            _gestureConfiguration.replica.AnimateTo(Mathf.Clamp01(t));
        }
    }
}