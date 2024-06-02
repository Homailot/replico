using System.Collections.Generic;
using System.Linq;
using Gestures.HandDetection;
using Gestures.ReplicaTransform;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.Balloon
{
    public class BalloonTeleportState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private readonly IReplicaPoint _replicaPoint;
        private readonly ArrowTransformer _arrowTransformer;
        
        private float _lastEmptyTime = 0;
        private bool _secondHandTwoFingers = true;
        private Hands _hands;
        
        public BalloonTeleportState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands, IReplicaPoint replicaPoint, ArrowTransformer arrowTransformer)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration; 
            _replicaPoint = replicaPoint;
            _handDetector = handDetector;
            _hands = hands;
            _arrowTransformer = arrowTransformer;
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
            
            if (_replicaPoint != null && _replicaPoint.Intersects())
            {
                _replicaPoint.Highlight();
            } 
            else
            {
                _replicaPoint?.Unhighlight();
            }
            
            if (_hands.secondHand.Count < 2 && _secondHandTwoFingers)
            {
                _secondHandTwoFingers = false;
            }
            
            var fingerPositions = Touch.activeFingers.Select(finger => finger.screenPosition).ToList();
            if (
                (
                    _hands.secondHand.Count > 1 || 
                    (
                        Touch.activeFingers.Count == 2 && 
                        Vector2.Distance(fingerPositions[0], fingerPositions[1]) / Mathf.Max(Screen.height, Screen.width) < _gestureConfiguration.handDistanceThreshold
                    )
                ) &&
                !_secondHandTwoFingers
            )
            {
                Teleport();
            }
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