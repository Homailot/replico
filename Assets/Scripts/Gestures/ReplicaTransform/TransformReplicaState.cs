using CustomCollections;
using Gestures.HandDetection;
using Gestures.Swipe;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.ReplicaTransform
{
    public class TransformReplicaState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private readonly ReplicaTransformer _replicaTransformer;

        private readonly OrderedSet<Finger> _fingerQueue = new(new FingerEqualityComparer());
        
        private float _timeSinceLastTouch;
        private float _timeSinceHandsDetected;
        private bool _handsMoved;

        private Hands _hands;
        private Vector2 _lastFirstHandCenter;
        private Vector2 _lastSecondHandCenter;
        
        public TransformReplicaState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = new HandDetector(2, _gestureConfiguration.handDistanceThreshold);
            _replicaTransformer = new ReplicaTransformer(_gestureConfiguration);
            _hands = Hands.none;
        }

        private bool DetectHandsStill(Hands hands)
        {
            if (hands.IsEmpty() || _handsMoved) return false;
           
            var firstHandCenter = hands.GetFirstHandCenter();
            var secondHandCenter = hands.GetSecondHandCenter();
            
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            var firstHandMoved = Vector2.Distance(firstHandCenter, _lastFirstHandCenter) / screenMax > _gestureConfiguration.handMovementDetectionDistance;
            var secondHandMoved = Vector2.Distance(secondHandCenter, _lastSecondHandCenter) / screenMax > _gestureConfiguration.handMovementDetectionDistance;
            
            if (firstHandMoved || secondHandMoved)
            {
                _timeSinceHandsDetected = 0;
                _lastFirstHandCenter = firstHandCenter;
                _lastSecondHandCenter = secondHandCenter;
                _handsMoved = true;
                Debug.Log("Hands moved");
                return false;
            }
            _timeSinceHandsDetected += Time.deltaTime;

            return _timeSinceHandsDetected > _gestureConfiguration.handMovementDetectionTime;
        }
       
        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == _gestureConfiguration.swipeFingers && _timeSinceLastTouch <= _gestureConfiguration.swipeGestureTimeDetection)
            {
                _gestureDetector.SwitchState(new SwipeDownReplicaGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            if (Touch.activeFingers.Count == 0) {
                _fingerQueue.Clear();
                _hands = Hands.none;
                _timeSinceLastTouch = 0;
                _timeSinceHandsDetected = 0;
                _handsMoved = false;
                return;
            }
            _timeSinceLastTouch += Time.deltaTime;

            while (_fingerQueue.Count != 0 && (!_fingerQueue.GetFirst()?.isActive ?? false))
            {
                _fingerQueue.RemoveFirst();
            }
            
            foreach (var finger in Touch.activeFingers)
            {
                _fingerQueue.Add(finger);
            }

            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands, _fingerQueue);
            if (!hands.IsEmpty())
            {
                if (_hands.IsEmpty())
                {
                     _timeSinceHandsDetected = 0;
                     _handsMoved = false;
                     _lastFirstHandCenter = hands.GetFirstHandCenter();
                     _lastSecondHandCenter = hands.GetSecondHandCenter();
                }

                var gestureDetected = DetectHandsStill(hands);

                if (gestureDetected)
                {
                    Debug.Log("Hands still detected");
                }
            }
            _hands = hands;
            
            Debug.Log(_hands.IsEmpty()
                ? "No hands detected"
                : $"Hands detected: {_hands.firstHand.Count} {_hands.secondHand.Count}");

            _replicaTransformer.Update(Touch.activeFingers);
        }

        public void OnEnter()
        {
            var replicaTransform = _gestureConfiguration.replicaController.GetReplica().transform;
            
            _gestureConfiguration.movementTarget.position = replicaTransform.position;
            _gestureConfiguration.movementTarget.rotation = replicaTransform.rotation;
            _gestureConfiguration.movementTarget.localScale = replicaTransform.localScale;
            _gestureConfiguration.replicaController.SetMovementTarget(_gestureConfiguration.movementTarget);
        }

        public void OnExit()
        {
            _gestureConfiguration.replicaController.SetMovementTarget(null);
        }
    }
}