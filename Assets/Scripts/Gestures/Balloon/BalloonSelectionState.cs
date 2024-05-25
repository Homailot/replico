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
        private float _initialDistance;
        private float _startingValue;
        private Vector2 _lastDirection;
        private Hands _hands;
        private float _lastDistance;
        
        private bool _lastEmpty = false;
        private float _lastEmptyTime = 0;
        
        private IReplicaPoint _lastReplicaPoint;
        
        public BalloonSelectionInitialState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _hands = hands;
            _lastDirection = hands.secondHand.First().screenPosition - hands.firstHand.First().screenPosition;
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            _initialDistance = Vector2.Distance(hands.firstHand.First().screenPosition / screenMax, hands.secondHand.First().screenPosition / screenMax);
            _lastDistance = _initialDistance;
            _startingValue = _gestureDetector.lastBalloonHeight;
        }

        private float GetValueFromDistance(float distance)
        {
            return Mathf.Max(_initialDistance - distance + _startingValue, 0) * _gestureConfiguration.balloonDistanceMultiplier;
        }
        
        public void OnUpdate()
        {
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            if (hands.secondHand.Count > 1)
            {
                _gestureDetector.ResetBalloonPlanePositions();
                _gestureDetector.SwitchState(new BalloonHoldState(_gestureDetector, _gestureConfiguration, _handDetector, hands, _lastReplicaPoint));
                return;
            }
            
            if (hands.IsEmpty())
            {
                if (_lastEmptyTime == 0)
                {
                    _lastEmptyTime = Time.time;
                }
                else if (Time.time - _lastEmptyTime > _gestureConfiguration.balloonSelectionTimeEmptyThreshold)
                {
                    Cancel();
                    return;
                }
            }
            else
            {
                _lastEmptyTime = 0;
                _hands = hands;
            }

            if (_hands.firstHand.Count < 1)
            {
                _lastEmpty = _hands.secondHand.Count < 1;
                return;
            }
            
            var secondHandPosition = _hands.firstHand.First().screenPosition + _lastDirection;
            var distance = _lastDistance;
            if (_hands.secondHand.Count >= 1)
            {
                secondHandPosition = _hands.secondHand.Last().screenPosition;
                var screenMax = Mathf.Max(Screen.width, Screen.height);
                distance = Vector2.Distance(_hands.firstHand.First().screenPosition / screenMax, secondHandPosition / screenMax); 
                // TODO: this could be a state, but im lazy :D
                if (_lastEmpty)
                {
                    _startingValue = GetValueFromDistance(_lastDistance);
                    _initialDistance = distance;
                }
                
                _gestureDetector.ToggleBalloonPlaneLine(true);
            }
            else
            {
                _gestureDetector.ToggleBalloonPlaneLine(false);
            }
            
            _gestureDetector.UpdateBalloonPlanePositions(
                _hands.firstHand.First().screenPosition, 
                secondHandPosition);
            var balloonScreenPosition = _hands.firstHand.First().screenPosition;
            _gestureDetector.UpdateBalloonPosition(new Vector3(balloonScreenPosition.x, GetValueFromDistance(distance), balloonScreenPosition.y));
            _gestureDetector.lastBalloonHeight = GetValueFromDistance(distance);

            var replicaPoint = _gestureDetector.GetReplicaPointFromBalloon();
            if (replicaPoint != null)
            {
                if (_lastReplicaPoint != null && _lastReplicaPoint != replicaPoint)
                {
                    _lastReplicaPoint.Unhighlight();
                }
                replicaPoint.Highlight();
                _lastReplicaPoint = replicaPoint;
            } else if (_lastReplicaPoint != null)
            {
                _lastReplicaPoint.Unhighlight();
                _lastReplicaPoint = null;
            }
            
            _lastEmpty = _hands.secondHand.Count < 1;
            _lastDirection = secondHandPosition - _hands.firstHand.First().screenPosition;
            _lastDistance = distance;
        }

        private void Cancel()
        {
            _gestureConfiguration.logger.EndBalloonSelection();
            _gestureDetector.OnGestureExit();
            _gestureDetector.ResetBalloonPlanePositionsAndHeight();
            _gestureDetector.DisableBalloon();

            _lastReplicaPoint?.Unhighlight();
            _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
        }
    }
}