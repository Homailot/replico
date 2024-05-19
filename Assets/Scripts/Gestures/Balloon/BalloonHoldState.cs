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
        private readonly IReplicaPoint _replicaPoint;
        private readonly ArrowTransformer _arrowTransformer;
        private readonly int _fingerId;

        private float _fingerCenter;
        private float _lastFinger;
        private Hands _hands;
        private bool _hasMoved = false;
        private float _holdTime;
        
        private bool _enableArrow = false;
        
        public BalloonHoldState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands, IReplicaPoint replicaPoint)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
            _replicaPoint = replicaPoint;
            _holdTime = Time.time;
            _handDetector = handDetector;
            _hands = hands;
            _lastFinger = hands.firstHand.First().screenPosition.y;
            _fingerCenter = hands.firstHand.First().screenPosition.y;
            _fingerId = hands.firstHand.First().index;
            _arrowTransformer = new ArrowTransformer(gestureConfiguration, gestureDetector, gestureConfiguration.balloonRotationSpeed);
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            _arrowTransformer.Update(Touch.activeFingers);

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

                //var distance = (finger.screenPosition.y - _lastFinger) / Screen.height;
                //var yRotation = distance * -_gestureConfiguration.balloonRotationSpeed;
                //_lastFinger = finger.screenPosition.y;
                //_gestureDetector.RotateBalloonArrow(yRotation);
                break;
            }

            if (_replicaPoint != null && _replicaPoint.Intersects())
            {
                _replicaPoint.Highlight();
            } 
            else
            {
                _replicaPoint?.Unhighlight();
            }
            
            var timeDifference = Time.time - _holdTime;
            
            if (timeDifference >= _gestureConfiguration.balloonShowArrowTime && !_enableArrow)
            {
                _enableArrow = true;
                _gestureDetector.EnableBalloonArrow();
            }
            
            _gestureDetector.SetBalloonProgress(timeDifference / _gestureConfiguration.balloonTeleportTime);
            
            if ((Touch.activeFingers.Count == 0 || hands.firstHand.Count < 1 || hands.secondHand.Count < 2))
            {
                // Do selection
                if (!_enableArrow)
                {
                    _replicaPoint?.Unhighlight();

                    if (_replicaPoint is { selectable: true })
                    {
                        _replicaPoint.OnSelect(_gestureDetector);
                    }
                    else
                    {
                        _gestureDetector.OnPointSelected();
                    }
                    
                    _gestureConfiguration.logger.EndBalloonSelection();
                    _gestureDetector.ResetBalloonPlanePositionsAndHeight(); 
                    _gestureDetector.DisableBalloon();
                    _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
                    return;                   
                }

                if (timeDifference < _gestureConfiguration.balloonTeleportTime)
                {
                    _replicaPoint?.Unhighlight();

                    _gestureConfiguration.logger.EndBalloonSelection();
                    _gestureDetector.ResetBalloonPlanePositionsAndHeight();
                    _gestureDetector.DisableBalloon();
                    _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
                    return;
                }
            }

            if (timeDifference >= _gestureConfiguration.balloonTeleportTime)
            {
                Teleport();
                return;
            }

            _hands = hands;
        }

        private void Teleport()
        {
            _replicaPoint?.Unhighlight();
            
            _gestureConfiguration.logger.EndBalloonSelection();
            _gestureConfiguration.logger.Teleportation();
            _gestureDetector.ResetBalloonPlanePositionsAndHeight();
            _gestureDetector.OnTeleportSelected();
            _gestureDetector.DisableBalloon();
            _gestureDetector.SwitchState(new BalloonSelectedState(_gestureDetector, _gestureConfiguration));
        }
    }
}