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

        private readonly IDictionary<int, Vector2> _previousFirstHand = new Dictionary<int, Vector2>();
        private readonly IDictionary<int, Vector2> _previousSecondHand = new Dictionary<int, Vector2>();
        
        private Hands _hands;
        private float _timeSinceHandsDetected;
        private readonly ISet<Finger> _movedFingers = new HashSet<Finger>(new FingerEqualityComparer());

        public TransformReplicaHandState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, ReplicaTransformer replicaTransform, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _replicaTransformer = replicaTransform;
            _hands = hands;
            
            foreach (var finger in hands.firstHand)
            {
                _previousFirstHand.Add(finger.index, finger.screenPosition);
            }
            
            foreach (var finger in hands.secondHand)
            {
                _previousSecondHand.Add(finger.index, finger.screenPosition);
            }
        }

        private ISet<Finger> DetectHandMovement(IEnumerable<Finger> hand, bool firstHand)
        {
            var previousHand = firstHand ? _previousFirstHand : _previousSecondHand;
            var movedFingers = new HashSet<Finger>();
            
            foreach (var finger in hand)
            {
                if (_movedFingers.Contains(finger))
                {
                    movedFingers.Add(finger);
                    continue;
                }
                
                if (previousHand.TryGetValue(finger.index, out var previousFinger) &&
                    Vector2.Distance(finger.screenPosition, previousFinger) / Mathf.Max(Screen.width, Screen.height) >
                    _gestureConfiguration.handMovementDetectionDistance)
                {
                    movedFingers.Add(finger);
                }
            }
            
            return movedFingers;
        }
        
        private bool DetectVerticalGesture(Hands hands)
        {
            if (hands.IsEmpty()) return false;

            ISet<Finger> fingersToRemove = new HashSet<Finger>();
            foreach (var finger in _movedFingers)
            {
                if (!hands.firstHand.Contains(finger) && !hands.secondHand.Contains(finger))
                {
                    fingersToRemove.Add(finger);
                }
            }
            _movedFingers.ExceptWith(fingersToRemove);

            var movedFingers = DetectHandMovement(hands.secondHand, false);
            if (movedFingers.Count > 0)
            {
                _timeSinceHandsDetected = 0;
                _movedFingers.UnionWith(movedFingers);
                return false;
            }
            _timeSinceHandsDetected += Time.deltaTime;

            return _timeSinceHandsDetected > _gestureConfiguration.handMovementDetectionTime && hands.secondHand.Count >= 2;
        }
         
        public void OnUpdate()
        {
            _replicaTransformer.Update(Touch.activeFingers);
           
            if (DetectVerticalGesture(_hands))
            {
                _gestureDetector.OnGestureDetected();
                _gestureDetector.SwitchState(new TransformReplicaVerticalState(_gestureDetector, _gestureConfiguration, _handDetector, _hands));
                return;
                
               // _gestureDetector.OnGestureDetected();
               // _gestureDetector.UpdateBalloonPlanePositions(_hands.GetFirstHandCenter(), _hands.GetSecondHandCenter());
               // _gestureDetector.EnableBalloon();
               // _gestureDetector.SwitchState(new BalloonSelectionInitialState(_gestureDetector, _gestureConfiguration, _handDetector, hands));
                return;
            }

            var secondHandMoved = _hands.secondHand.Any(finger => _movedFingers.Contains(finger));

            _hands = secondHandMoved ? _handDetector.DetectHands(Touch.activeFingers, _hands, false) : _handDetector.DetectHands(Touch.activeFingers, _hands);
            _hands.Print();
            
            if (_hands.IsEmpty() || _hands.firstHand.Count < 1 || _hands.secondHand.Count < 1)
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
            }
        }
    }
}