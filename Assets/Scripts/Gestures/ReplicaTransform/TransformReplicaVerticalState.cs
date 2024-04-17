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
        
        private Hands _hands;

        public TransformReplicaVerticalState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _replicaTransformer = new ReplicaTransformer(_gestureConfiguration);
            _hands = hands;
            Debug.Log("in vertical");
        }

        public void OnUpdate()
        {
            var fingerArray = new ReadOnlyArray<Finger>(_hands.secondHand.ToArray());
            _replicaTransformer.Update(fingerArray, true);
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);

            if (hands.IsEmpty())
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            if (hands.firstHand.Count < 2)
            {
                _gestureDetector.SwitchState(new TransformReplicaHandState(_gestureDetector, _gestureConfiguration, _handDetector, _replicaTransformer, hands));
                return;
            }
            
            _hands = hands;
        }
    }
}