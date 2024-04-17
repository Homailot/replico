using Gestures.HandDetection;
using UnityEngine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.ReplicaTransform
{
    public class TransformReplicaHandState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private readonly ReplicaTransformer _replicaTransformer;
        private readonly Hands _firstHands;
        
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
            _firstHands = hands;
        }
        
        private bool DetectHandsStill(Hands hands)
        {
            if (hands.IsEmpty() || _handsMoved) return false;
           
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            foreach (var finger in hands.firstHand)
            {
                if (!_firstHands.firstHand.TryGetValue(finger, out var previousFinger) ||
                    !(Vector2.Distance(finger.screenPosition, previousFinger.screenPosition) / screenMax >
                      _gestureConfiguration.handMovementDetectionDistance)) continue;
                _timeSinceHandsDetected = 0;
                _handsMoved = true;
                return false;
            }

            foreach (var finger in hands.secondHand)
            {
                if (!_firstHands.secondHand.TryGetValue(finger, out var previousFinger) ||
                    !(Vector2.Distance(finger.screenPosition, previousFinger.screenPosition) / screenMax >
                      _gestureConfiguration.handMovementDetectionDistance)) continue;
                _timeSinceHandsDetected = 0;
                _handsMoved = true;
                return false;
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
                Debug.Log("Hands still");
            }
            
            Debug.Log($"Hands detected: {hands.firstHand.Count} {hands.secondHand.Count}");
            foreach (var finger in hands.firstHand)
            {
                Debug.Log($"First hand: {finger.screenPosition}");
            }
            
            foreach (var finger in hands.secondHand)
            {
                Debug.Log($"Second hand: {finger.screenPosition}");
            }
            
            _hands = hands;
        }
    }
}