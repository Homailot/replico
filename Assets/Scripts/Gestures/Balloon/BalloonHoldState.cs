using System.Collections.Generic;
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
        private readonly int _fingerId;

        private float _fingerCenter;
        private float _lastFinger;
        private Hands _hands;
        private bool _hasMoved = false;
        private float _holdTime;
        
        private bool _enableArrow = false;
        
        public BalloonHoldState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
            _holdTime = Time.time;
            _handDetector = handDetector;
            _hands = hands;
            _lastFinger = hands.firstHand.First().screenPosition.y;
            _fingerCenter = hands.firstHand.First().screenPosition.y;
            _fingerId = hands.firstHand.First().index;
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);

            foreach (var finger in hands.firstHand)
            {
                if (finger.index != _fingerId) continue;
                
                if ((Mathf.Abs(finger.screenPosition.y - _fingerCenter) / Screen.height) > _gestureConfiguration.balloonMovementDetectionDistance)
                {
                    if (!_hasMoved && !_enableArrow)
                    {
                        _gestureDetector.EnableBalloonArrow(); 
                        _enableArrow = true;
                    }
                    _hasMoved = true;
                    
                    _fingerCenter = finger.screenPosition.y;
                    _holdTime = Time.time;
                }

                var distance = (finger.screenPosition.y - _lastFinger) / Screen.height;
                var yRotation = distance * -_gestureConfiguration.balloonRotationSpeed;
                _lastFinger = finger.screenPosition.y;
                _gestureDetector.RotateBalloonArrow(yRotation);
                break;
            }
            
            
            var timeDifference = Time.time - _holdTime;
            
            if (timeDifference >= _gestureConfiguration.balloonShowArrowTime && !_enableArrow)
            {
                _enableArrow = true;
                _gestureDetector.EnableBalloonArrow();
            }
            
            _gestureDetector.SetBalloonProgress(timeDifference / _gestureConfiguration.balloonTeleportTime);
            
            // in any case, either teleport or select, no turning back!
            if ((Touch.activeFingers.Count == 0 || hands.firstHand.Count < 1 || hands.secondHand.Count < 2) && timeDifference < _gestureConfiguration.balloonTeleportTime)
            {
                if (_hasMoved)
                {
                    Teleport();
                    return;
                }
                // Do selection
                _gestureDetector.ResetBalloonPlanePositionsAndHeight();
                _gestureDetector.OnPointSelected();
                _gestureDetector.DisableBalloon();
                _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
                return;
            }

            if (timeDifference >= _gestureConfiguration.balloonTeleportTime)
            {
                Teleport();
            }

            _hands = hands;
        }

        private void Teleport()
        {
            _gestureDetector.ResetBalloonPlanePositionsAndHeight();
            _gestureDetector.OnTeleportSelected();
            _gestureDetector.DisableBalloon();
            _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
        }
    }
}