using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Utils;

namespace Gestures.ReplicaTransform
{
    public abstract class TouchTransformer
    {
        protected readonly GestureConfiguration _gestureConfiguration;
        
        protected Vector2 _lastCenter;
        protected float _lastDistance;
        protected readonly Dictionary<Finger, Vector2> _lastFingerPositions = new(new FingerEqualityComparer());

        protected TouchTransformer(GestureConfiguration gestureConfiguration)
        {
            _gestureConfiguration = gestureConfiguration;
        }
        
        public static Vector2 CalculateCenter(IReadOnlyList<Finger> touches)
        {
            var tMax = touches[0].screenPosition;
            var tMin = tMax;

            for (var i = 1; i < touches.Count; i++)
            {
                var tPos = touches[i].screenPosition;
                tMax = new Vector2(Mathf.Max(tMax.x, tPos.x), Mathf.Max(tMax.y, tPos.y));
                tMin = new Vector2(Mathf.Min(tMin.x, tPos.x), Mathf.Min(tMin.y, tPos.y));
            }

            return (tMin + tMax) / 2.0f;
        }

        public static float CalculateAverageDistance(IReadOnlyCollection<Finger> touches, Vector2 center)
        {
            float avgDistance = 0;
            foreach (var finger in touches)
            {
                avgDistance += (center - finger.screenPosition).magnitude;
            }
            avgDistance /= touches.Count;

            return avgDistance;
        }
        
        private float CalculateAverageRotation(IReadOnlyCollection<Finger> touches, Vector2 center, Vector2 lastCenter, int lastTouchCount)
        {
            float avgRotation = 0;
            if (lastTouchCount != touches.Count || touches.Count <= 1) return avgRotation;
            
            foreach (var finger in touches)
            {
                var oldDir = _lastFingerPositions[finger] - lastCenter;
                var newDir = finger.screenPosition - center;
                var angle = Vector2.Angle(oldDir, newDir);
                
                if (Vector3.Cross(oldDir, newDir).z < 0) angle = -angle;
                avgRotation += angle;
            }
            avgRotation /= touches.Count;

            return avgRotation;
        }
        
        public void Update(ReadOnlyArray<Finger> touches)
        {
            if (touches.Count == 0)
            {
                _lastFingerPositions.Clear();
                _lastCenter = Vector2.zero;
                _lastDistance = 0;
                return;
            }
            
            if (_lastFingerPositions.Count == 0)
            {
                foreach (var finger in touches)
                {
                    _lastFingerPositions.Add(finger, finger.screenPosition);
                }
                _lastCenter = CalculateCenter(touches);
                _lastDistance = CalculateAverageDistance(touches, _lastCenter);
                return;
            }
            
            var touchCount = touches.Count;
            var touchCenter = CalculateCenter(touches);
            var touchDistance = CalculateAverageDistance(touches, touchCenter);
            var touchRotation = CalculateAverageRotation(touches, touchCenter, _lastCenter, _lastFingerPositions.Count);
            
            if (_lastFingerPositions.Count == touchCount)
            {
                OnUpdate(touchCount, touchCenter, touchDistance, touchRotation);
            }
           
            _lastFingerPositions.Clear();
            foreach (var finger in touches)
            {
                _lastFingerPositions.Add(finger, finger.screenPosition);
            }
            _lastCenter = touchCenter;
            _lastDistance = touchDistance;
        }
        
        public abstract void OnUpdate(int touchCount, Vector2 touchCenter, float touchDistance, float touchRotation);
    }
}