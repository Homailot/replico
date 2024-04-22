using System.Linq;
using Gestures.HandDetection;
using Gestures.ReplicaTransform;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.Balloon
{
    public class BalloonSelectionInitialState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private Vector2 _lastSecondHandPosition;
        private Hands _hands;
        
        public BalloonSelectionInitialState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _hands = hands;
            _lastSecondHandPosition = hands.secondHand.First().screenPosition;
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            if (hands.IsEmpty() || hands.firstHand.Count < 1)
            {
                _gestureDetector.ResetBalloonPlanePositions();
                _gestureDetector.DisableBalloon();
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            _hands = hands;

            var secondHandPosition = _lastSecondHandPosition;
            if (_hands.secondHand.Count >= 1)
            {
                secondHandPosition = _hands.secondHand.Last().screenPosition;
                _lastSecondHandPosition = secondHandPosition;
            }
            
            _gestureDetector.UpdateBalloonPlanePositions(
                _hands.firstHand.First().screenPosition, 
                secondHandPosition);
            var balloonScreenPosition = _hands.firstHand.First().screenPosition;
            _gestureDetector.UpdateBalloonPosition(new Vector3(balloonScreenPosition.x, 0, balloonScreenPosition.y));
        }
    }
}