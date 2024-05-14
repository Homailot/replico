using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Utils;

namespace Gestures.ReplicaTransform
{
    public class ReplicaTransformer : TouchTransformer
    {
        private readonly bool _vertical;
        
        public ReplicaTransformer(GestureConfiguration gestureConfiguration, bool vertical = false) : base(gestureConfiguration)
        {
            _vertical = vertical;
        }
        
        public override void OnUpdate(int touchCount, Vector2 touchCenter, float touchDistance, float touchRotation) 
        {
            if (touchCount > 1 && !_vertical)
            {
                var scale = touchDistance / _lastDistance;
                scale = Mathf.Pow(scale, _gestureConfiguration.scaleSpeed);
                _gestureConfiguration.movementTarget.localScale *= scale;
            }

            var touchPlaneFingerPosition = _gestureConfiguration.touchToPosition.GetTouchPosition(touchCenter);
            if (!_vertical)
            {
                _gestureConfiguration.movementTarget.RotateAround(touchPlaneFingerPosition, Vector3.up, -touchRotation);
            }
            var movement = new Vector3(
                (touchCenter.x - _lastCenter.x) * _gestureConfiguration.translateSpeed, 
                _vertical ? (touchCenter.y - _lastCenter.y) * _gestureConfiguration.translateSpeed : 0,
                _vertical ? 0 : (touchCenter.y - _lastCenter.y) * _gestureConfiguration.translateSpeed
                );
            var transformedMovement = _gestureConfiguration.frame.TransformVector(movement);
            _gestureConfiguration.movementTarget.position += transformedMovement;
            
            _gestureConfiguration.logger.UpdateReplicaTransform(_gestureConfiguration.movementTarget.localPosition, _gestureConfiguration.movementTarget.localRotation, _gestureConfiguration.movementTarget.localScale);
        }
    }
}