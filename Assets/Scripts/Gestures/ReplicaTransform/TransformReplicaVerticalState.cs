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

        public TransformReplicaVerticalState(GestureDetector gestureDetector, GestureConfiguration gestureConfiguration, HandDetector handDetector, Hands hands)
        {
            _gestureDetector = gestureDetector;
            _gestureConfiguration = gestureConfiguration;
            _handDetector = handDetector;
            _replicaTransformer = new ReplicaTransformer(_gestureConfiguration);
            _replicaTransformerVertical = new ReplicaTransformer(_gestureConfiguration, true);
            _hands = hands;
            Debug.Log("in vertical");
        }

        public void OnUpdate()
        {
            var firstHandArray = new ReadOnlyArray<Finger>(_hands.firstHand.ToArray());
            var fingerArray = new ReadOnlyArray<Finger>(_hands.secondHand.ToArray());
            
            _replicaTransformer.Update(firstHandArray);
            _replicaTransformerVertical.Update(fingerArray);
            var hands = _handDetector.DetectHands(Touch.activeFingers, _hands);
            hands.Print();

            if (hands.IsEmpty())
            {
                _gestureDetector.SwitchState(new TransformReplicaInitialState(_gestureDetector, _gestureConfiguration));
                return;
            }
            
            // what if it's better to not switch state if one of the hands is missing?
           // if (hands.secondHand.Count < 1)
           // {
           //     _gestureDetector.SwitchState(new TransformReplicaHandState(_gestureDetector, _gestureConfiguration, _handDetector, _replicaTransformer, hands));
           //     return;
           // }
            
            _hands = hands;
        }
    }
}