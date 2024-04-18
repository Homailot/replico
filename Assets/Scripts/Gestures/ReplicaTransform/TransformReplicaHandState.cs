using System.Collections.Generic;
using System.Linq;
using Gestures.Balloon;
using Gestures.HandDetection;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.ReplicaTransform
{
    public class TransformReplicaHandState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private readonly ReplicaTransformer _replicaTransformer;

        private readonly Dictionary<Finger, Vector2> _previousFirstHand = new(new FingerEqualityComparer());
        private readonly Dictionary<Finger, Vector2> _previousSecondHand = new(new FingerEqualityComparer());
        
        private Hands _hands;
        private float _timeSinceHandsDetected;
        private bool _handsMoved;

        public TransformReplicaHandState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, ReplicaTransformer replicaTransform, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _replicaTransformer = replicaTransform;
            _hands = hands;
            
            foreach (var finger in hands.firstHand)
            {
                _previousFirstHand.Add(finger, finger.screenPosition);
            }
            
            foreach (var finger in hands.secondHand)
            {
                _previousSecondHand.Add(finger, finger.screenPosition);
            }
        }
        
        private bool DetectHandsStill(Hands hands)
        {
            if (hands.IsEmpty() || _handsMoved) return false;
           
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            foreach (var finger in hands.firstHand)
            {
                if (_previousFirstHand.TryGetValue(finger, out var previousFinger) &&
                    Vector2.Distance(finger.screenPosition, previousFinger) / screenMax >
                    _gestureConfiguration.handMovementDetectionDistance)
                {
                    _timeSinceHandsDetected = 0;
                    _handsMoved = true;
                    return false;
                }
            }

            foreach (var finger in hands.secondHand)
            {
                // TODO: gonna relax this check for now, seems more friendly to the user
                // perhaps allow the second hand to be added later on
                //if (_previousSecondHand.TryGetValue(finger, out var previousFinger) &&
                //    Vector2.Distance(finger.screenPosition, previousFinger) / screenMax >
                //    _gestureConfiguration.handMovementDetectionDistance)
                //{
                //    _timeSinceHandsDetected = 0;
                //    _handsMoved = true;
                //    return false;
                //}
            }
            _timeSinceHandsDetected += Time.deltaTime;

            return _timeSinceHandsDetected > _gestureConfiguration.handMovementDetectionTime;
        }
         
        public void OnUpdate()
        {
            _replicaTransformer.Update(Touch.activeFingers);
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);

            if (hands.IsEmpty())
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            } 
            
            if (DetectHandsStill(hands))
            {
                if (hands.firstHand.Count >= 2)
                {
                    _gestureDetector.OnGestureDetected();
                    _gestureDetector.SwitchState(new TransformReplicaVerticalState(_gestureDetector, _gestureConfiguration, _handDetector, hands));
                    return;
                }
                
                _gestureDetector.OnGestureDetected();
                _gestureDetector.UpdateBalloonPlanePositions(_hands.GetFirstHandCenter(), _hands.GetSecondHandCenter());
                _gestureDetector.SwitchState(new BalloonSelectionInitialState(_gestureDetector, _gestureConfiguration, _handDetector, hands));
                return;
            }
            
            _hands = hands;
        }
    }
}