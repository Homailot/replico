using System.Linq;
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
        
        private float _timeSinceLastTouch;
       
        public TransformReplicaState(GestureDetector gestureDetector, ReplicaTransformer replicaTransformer, GestureConfiguration gestureConfiguration, HandDetector handDetector = null)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector ?? new HandDetector(2, _gestureConfiguration.handDistanceThreshold);
            _replicaTransformer = replicaTransformer;
        }

        public void OnUpdate()
        {
            if (Touch.activeFingers.Count == 0)
            {
                _gestureConfiguration.logger.EndTransform();
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            if (Touch.activeFingers.Count == _gestureConfiguration.swipeFingers && _timeSinceLastTouch <= _gestureConfiguration.swipeGestureTimeDetection)
            {
                _gestureConfiguration.logger.EndTransform();
                _gestureConfiguration.replicaController.SetMovementTarget(null);
                _gestureDetector.SwitchState(new SwipeDownReplicaGesture(_gestureDetector, _gestureConfiguration));
                return;
            }
            _timeSinceLastTouch += Time.deltaTime;

            _replicaTransformer.Update(Touch.activeFingers);
            var hands = _handDetector.DetectHands(Touch.activeFingers, Hands.none);
            if (!hands.IsEmpty() && _timeSinceLastTouch > _gestureConfiguration.swipeGestureTimeDetection)
            {
                _gestureDetector.SwitchState(new TransformReplicaHandState(
                    _gestureDetector,
                    _gestureConfiguration,
                    _handDetector,
                    _replicaTransformer,
                    hands
                ));
            }
        }

    }
}