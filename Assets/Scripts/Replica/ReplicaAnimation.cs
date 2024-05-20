using System;
using System.Collections;
using UnityEngine;

namespace Replica
{
    public class ReplicaAnimation : MonoBehaviour
    {
        [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        // Duration used for single events, such as a swipe being cancelled. In seconds
        [SerializeField] private float duration;
        [SerializeField] private float startAlpha;
        [SerializeField] private float endAlpha;
        [SerializeField] private Transform startTransform;
        [SerializeField] private Transform endTransform;

        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private Vector3 _startScale;
        private Vector3 _endScale;
        private Quaternion _startRotation;
        private Quaternion _endRotation;
    
        private ReplicaController _replicaController;

        private float _t;
        private Coroutine _currentCoroutine;

        private void Awake()
        {
            _replicaController = GetComponent<ReplicaController>();
            _t = 0.0f;

            _startPosition = startTransform.position;
            _endPosition = endTransform.position;
            _startScale = startTransform.localScale;
            _endScale = endTransform.localScale;
            _startRotation = startTransform.rotation;
            _endRotation = endTransform.rotation;
        }

        public void SetStartTransform(Transform start)
        {
            _startPosition = start.position;
            _startScale = start.localScale;
            _startRotation = start.rotation;
        }
    
        public void SetEndTransform(Transform end)
        {
            _endPosition = end.position;
            _endScale = end.localScale;
            _endRotation = end.rotation;
        }
        
        public Transform GetEndTransform()
        {
            return endTransform;
        }
    
        public void ResetTransforms()
        {
            _startPosition = startTransform.position;
            _endPosition = endTransform.position;
            _startScale = startTransform.localScale;
            _endScale = endTransform.localScale;
            _startRotation = startTransform.rotation;
            _endRotation = endTransform.rotation;
        }

        public void AnimateTo(float t)
        {
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }
        
            var startTransformPosition = _startPosition;
            var endTransformPosition = _endPosition;
            var startTransformScale = _startScale;
            var endTransformScale = _endScale;
            var startQuaternion = _startRotation;
            var endQuaternion = _endRotation;
        
            var curveValue = animationCurve.Evaluate(t);
            var replicaTransform = _replicaController.GetReplica().transform;
            replicaTransform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
            replicaTransform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
            replicaTransform.rotation = Quaternion.Lerp(startQuaternion, endQuaternion, curveValue);
        
            _t = t;
        }

        public void RevertAnimation(Action onComplete = null)
        {
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }
        
            _currentCoroutine = StartCoroutine(AnimateToCoroutine(0.0f, onComplete));
        }
    
        public void CompleteAnimation(Action onComplete = null)
        {
            if (_currentCoroutine != null)
            {
                StopCoroutine(_currentCoroutine);
            }
        
            _currentCoroutine = StartCoroutine(AnimateToCoroutine(1.0f, onComplete));
        }

        private IEnumerator AnimateToCoroutine(float t, Action onComplete = null)
        {
            var currentTime = 0.0f;
            var startTransformPosition = _startPosition;
            var endTransformPosition = _endPosition;
            var startTransformScale = _startScale;
            var endTransformScale = _endScale;
            var startQuaternion = _startRotation;
            var endQuaternion = _endRotation;

            var replicaTransform = _replicaController.GetReplica().transform;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                var curveValue = animationCurve.Evaluate(_t + (currentTime / duration) * (t - _t));
            
                replicaTransform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, curveValue);
                replicaTransform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, curveValue);
                replicaTransform.rotation = Quaternion.Lerp(startQuaternion, endQuaternion, curveValue);
            
                yield return null;
            }
        
            replicaTransform.position = Vector3.Lerp(startTransformPosition, endTransformPosition, t);
            replicaTransform.localScale = Vector3.Lerp(startTransformScale, endTransformScale, t);
            replicaTransform.rotation = Quaternion.Lerp(startQuaternion, endQuaternion, t);

            _currentCoroutine = null;
            _t = t;
            onComplete?.Invoke();
        }
    }
}