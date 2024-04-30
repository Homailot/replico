using System.Linq;
using Gestures.HandDetection;
using Gestures.ReplicaTransform;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.Balloon
{
    public class BalloonHoldState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private Hands _hands;
        private readonly float _holdTime;
        
        public BalloonHoldState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
            _holdTime = Time.time;
            _handDetector = handDetector;
            _hands = hands;
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            var timeDifference = Time.time - _holdTime;
            
            _gestureDetector.SetBalloonProgress(timeDifference / _gestureConfiguration.balloonTeleportTime);
            
            // in any case, either teleport or select, no turning back!
            if ((Touch.activeFingers.Count == 0 || hands.firstHand.Count < 1 || hands.secondHand.Count < 2) && timeDifference < _gestureConfiguration.balloonTeleportTime)
            {
                // Do selection
                _gestureDetector.ResetBalloonPlanePositionsAndHeight();
                _gestureDetector.OnPointSelected();
                _gestureDetector.DisableBalloon();
                _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
                return;
            }

            if (timeDifference >= _gestureConfiguration.balloonTeleportTime)
            {
                _gestureDetector.ResetBalloonPlanePositionsAndHeight();
                _gestureDetector.OnTeleportSelected();
                _gestureDetector.DisableBalloon();
                _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
            }

            _hands = hands;
        }
    }
}