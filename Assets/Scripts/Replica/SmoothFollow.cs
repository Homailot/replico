using UnityEngine;

namespace Replica
{
    public class SmoothFollow : MonoBehaviour
    {
        [SerializeField] private AnimationCurve translationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 5.0f, 1.0f);
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 2.0f * Mathf.PI, 1.0f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        
        private ReplicaController _replicaController;
        private Transform _target;

        private void Start()
        {
            _replicaController = GetComponent<ReplicaController>();
        }

        private void Update()
        {
            if (_target == null)
            {
                return;
            }

            var replicaTransform = _replicaController.GetReplica().transform;
            
            var position = replicaTransform.position;
            var localScale = replicaTransform.localScale;
            var rotation = replicaTransform.rotation;
            
            var targetPosition = _target.position;
            var targetLocalScale = _target.localScale;
            var targetRotation = _target.rotation;
            
            var distance = Vector3.Distance(position, targetPosition);
            var scaleDistance = Vector3.Distance(localScale, targetLocalScale);
            var rotationDistance = Quaternion.Angle(rotation, targetRotation);
            
            var translationT = translationCurve.Evaluate(distance);
            var scaleT = scaleCurve.Evaluate(scaleDistance);
            var rotationT = rotationCurve.Evaluate(rotationDistance);
            
            position = Vector3.Lerp(position, targetPosition, translationT);
            localScale = Vector3.Lerp(localScale, targetLocalScale, scaleT);
            rotation = Quaternion.Lerp(rotation, targetRotation, rotationT);
            
            replicaTransform.position = position;
            replicaTransform.localScale = localScale;
            replicaTransform.rotation = rotation;
        }
        
        public void SetTarget(Transform target)
        {
            _target = target;
        }
    }
}
