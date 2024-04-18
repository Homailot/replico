using Gestures.HandDetection;
using Gestures.ReplicaTransform;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures.Balloon
{
    public class BalloonSelectionInitialState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private Hands _hands;
        
        public BalloonSelectionInitialState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _hands = hands;
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            if (hands.IsEmpty() || hands.firstHand.Count < 1 || hands.secondHand.Count < 1)
            {
                _gestureDetector.ResetBalloonPlanePositions();
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            _hands = hands; 
            _gestureDetector.UpdateBalloonPlanePositions(_hands.GetFirstHandCenter(), _hands.GetSecondHandCenter());
        }
    }
}