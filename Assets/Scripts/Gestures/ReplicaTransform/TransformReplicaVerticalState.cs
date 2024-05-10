using System.Linq;
using Gestures.HandDetection;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures.ReplicaTransform
{
    public class TransformReplicaVerticalState : IGestureState
    {
        private readonly GestureDetector _gestureDetector;
        private readonly GestureConfiguration _gestureConfiguration;
        private readonly HandDetector _handDetector;
        private readonly ReplicaTransformer _replicaTransformer;
        private readonly ReplicaTransformer _replicaTransformerVertical;
        
        private Hands _hands;
        private float _timeSinceSecondHandEmpty;

        public TransformReplicaVerticalState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _replicaTransformer = new ReplicaTransformer(_gestureConfiguration);
            _replicaTransformerVertical = new ReplicaTransformer(_gestureConfiguration, true);
            _hands = hands;
        }

        public void OnUpdate()
        {
            var firstHandArray = new ReadOnlyArray<Finger>(_hands.firstHand.ToArray());
            var fingerArray = new ReadOnlyArray<Finger>(_hands.secondHand.ToArray());
            
            _replicaTransformer.Update(firstHandArray);
            _replicaTransformerVertical.Update(fingerArray);
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            
            if (hands.secondHand.Count == 0)
            {
                _timeSinceSecondHandEmpty += Time.deltaTime;
            }
            else
            {
                _timeSinceSecondHandEmpty = 0;
            }
            
            if (_timeSinceSecondHandEmpty > _gestureConfiguration.verticalGestureHandEmptyAllowance || hands.IsEmpty())
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            _hands = hands;
        }

        public void OnExit()
        {
            _gestureDetector.OnGestureExit();
        }
    }
}