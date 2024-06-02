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
        private float _timeSinceHandsDetectedBalloon;
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

            var movedFingers = DetectHandMovement(hands.secondHand, false);
            if (movedFingers.Count > 0)
            {
                _timeSinceHandsDetected = 0;
                _movedFingers.UnionWith(movedFingers);
                return false;
            }

            return _timeSinceHandsDetected > _gestureConfiguration.handMovementDetectionTime && hands.secondHand.Count >= 2;
        }

        private bool DetectBalloonGesture(Hands hands)
        {
            if (hands.IsEmpty()) return false;
            
            var movedFingers = DetectHandMovement(hands.firstHand, true);
            var movedFingersSecondHand = DetectHandMovement(hands.secondHand, false);
            
            if (movedFingers.Count > 0 || movedFingersSecondHand.Count > 0)
            {
                _timeSinceHandsDetectedBalloon = 0;
                _movedFingers.UnionWith(movedFingers);
                _movedFingers.UnionWith(movedFingersSecondHand);
                return false;
            }
            
            return _timeSinceHandsDetectedBalloon > _gestureConfiguration.handMovementDetectionTime && hands.firstHand.Count == 1 && hands.secondHand.Count == 1;
        }
         
        public void OnUpdate()
        {
            _replicaTransformer.Update(Touch.activeFingers); 
            ISet<Finger> fingersToRemove = new HashSet<Finger>(); 
            foreach (var finger in _movedFingers) 
            { 
                if (!_hands.firstHand.Contains(finger) && !_hands.secondHand.Contains(finger)) 
                { 
                    fingersToRemove.Add(finger);
                }
            }
            _movedFingers.ExceptWith(fingersToRemove);

            if (DetectVerticalGesture(_hands))
            {
                _gestureConfiguration.logger.StartVerticalTransform();
                _gestureDetector.OnGestureDetected();
                _gestureDetector.SwitchState(new TransformReplicaVerticalState(_gestureDetector, _gestureConfiguration, _handDetector, _hands));
                return;
            }
            _timeSinceHandsDetected += Time.deltaTime;
            
            if (DetectBalloonGesture(_hands))
            {
                _gestureConfiguration.logger.EndTransform();
                _gestureConfiguration.logger.StartBalloonSelection();
                _gestureDetector.OnGestureDetected();
                _gestureDetector.UpdateBalloonPosition(_hands.GetFirstHandCenter());
                _gestureDetector.EnableBalloon();
                _gestureDetector.SwitchState(new BalloonSelectionInitialState(_gestureDetector, _gestureConfiguration, _handDetector, _hands));
                return;
            }
            _timeSinceHandsDetectedBalloon += Time.deltaTime;

            var secondHandMoved = _hands.secondHand.Any(finger => _movedFingers.Contains(finger));

            _hands = secondHandMoved ? _handDetector.DetectHands(Touch.activeFingers, _hands, false) : _handDetector.DetectHands(Touch.activeFingers, _hands);
            
            if (_hands.IsEmpty() || _hands.firstHand.Count < 1 || _hands.secondHand.Count < 1)
            {
                if (_hands.IsEmpty())
                {
                    _gestureConfiguration.logger.EndTransform();
                    _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                }
                else
                {
                    _gestureDetector.SwitchState(new TransformReplicaState(_gestureDetector, _replicaTransformer, _gestureConfiguration, _handDetector));
                }
            }
        }
    }
}