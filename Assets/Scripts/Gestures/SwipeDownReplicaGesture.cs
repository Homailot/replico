using UnityEngine;

namespace Gestures
{
    public class SwipeDownReplicaGesture : SwipeReplicaGesture
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        
        public SwipeDownReplicaGesture(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration) : base(gestureConfiguration)
        {
            swipeThreshold = -gestureConfiguration.swipeThreshold;
            gestureConfiguration.replica.AnimateTo(1.0f);
            gestureConfiguration.replica.EnableReplica();
            
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
        }

        protected override Vector2 swipeThreshold { get; }
        protected override void OnSwipeDetected()
        {
            _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
            _gestureConfiguration.replica.DisableReplica();
        }

        protected override void OnSwipeCancelled(float t)
        {
            if (t > _gestureConfiguration.swipeHalfThreshold)
            {
                _gestureConfiguration.replica.RevertAnimation(() => _gestureConfiguration.replica.DisableReplica());
                _gestureDetector.SwitchState(new InitialGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            _gestureConfiguration.replica.CompleteAnimation();
            _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _gestureConfiguration));
        }

        protected override void OnSwipeMoved(float t)
        {
            _gestureConfiguration.replica.AnimateTo(1 - Mathf.Clamp01(t));
        }
    }
}