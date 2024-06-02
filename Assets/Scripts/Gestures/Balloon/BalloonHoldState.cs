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
        private readonly Vector2 _fingerCenter;
        private readonly float _holdTime;
        
        private float _lastEmptyTime = 0;
        private Hands _hands;
        
        public BalloonHoldState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands, IReplicaPoint replicaPoint)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
            _replicaPoint = replicaPoint;
            _holdTime = Time.time;
            _handDetector = handDetector;
            _hands = hands;
            _fingerCenter = new Vector2(hands.firstHand.First().screenPosition.x,
                hands.firstHand.First().screenPosition.y);
            _fingerId = hands.firstHand.First().index;
            _arrowTransformer = new ArrowTransformer(gestureConfiguration, gestureDetector, gestureConfiguration.balloonRotationSpeed);
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            _arrowTransformer.Update(Touch.activeFingers);
           
            if (hands.IsEmpty())
            {
                if (_lastEmptyTime == 0)
                {
                    _lastEmptyTime = Time.time;
                }
                else if (Time.time - _lastEmptyTime > _gestureConfiguration.balloonSelectionTimeEmptyThreshold)
                {
                    _replicaPoint?.Unhighlight();
                    
                    _gestureConfiguration.logger.EndBalloonSelection(); 
                    _gestureDetector.ResetBalloonPlanePositionsAndHeight();
                    _gestureDetector.DisableBalloon();
                    _gestureDetector.OnGestureExit();
                    _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                    return;
                }
            }
            else
            {
                _lastEmptyTime = 0;
                _hands = hands;
            }
            
            foreach (var finger in _hands.firstHand)
            {
                if (finger.index != _fingerId) continue;
                
                if ( Vector2.Distance(finger.screenPosition, _fingerCenter) / Mathf.Max(Screen.height, Screen.width) > _gestureConfiguration.balloonMovementDetectionDistance)
                {
                    ToTeleportState();
                    return;
                }
                break;
            }

            var timeDifference = Time.time - _holdTime;
            if (timeDifference >= _gestureConfiguration.balloonShowArrowTime)
            {
                ToTeleportState();
                return;
            }
            
            if (_hands.secondHand.Count < 2)
            {
                // Do selection
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
            }
        }
        
        private void ToTeleportState()
        {
            _gestureDetector.ResetBalloonPlanePositionsAndHeight(); 
            _gestureDetector.EnableBalloonArrow();
            
            _gestureDetector.SwitchState(new BalloonTeleportState(_gestureDetector, _gestureConfiguration, _handDetector, _hands, _replicaPoint, _arrowTransformer)); 
        }
    }
}