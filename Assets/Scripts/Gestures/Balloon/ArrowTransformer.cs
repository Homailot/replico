using Gestures.ReplicaTransform;
using UnityEngine;

namespace Gestures.Balloon
{
    public class ArrowTransformer : TouchTransformer
    {
        private readonly GestureDetector _gestureDetector;
        private readonly float _rotationSpeed;
        
        public ArrowTransformer(GestureConfiguration gestureConfiguration, GestureDetector gestureDetector, float rotationSpeed) : base(gestureConfiguration)
        {
            _gestureDetector = gestureDetector;
            _rotationSpeed = rotationSpeed;
        }

        public override void OnUpdate(int touchCount, Vector2 touchCenter, float touchDistance, float touchRotation)
        {
            if (touchCount <= 1) return;
            
            var rotation = touchRotation * _rotationSpeed;
            _gestureDetector.RotateBalloonArrow(rotation); 
        }
    }
}