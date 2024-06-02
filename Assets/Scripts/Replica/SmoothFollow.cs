using UnityEngine;

namespace Replica
{
    public class SmoothFollow : MonoBehaviour
    {
        [SerializeField] private AnimationCurve translationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 5.0f, 1.0f);
        [SerializeField] private AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 2.0f * Mathf.PI, 1.0f);
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        
        [SerializeField] private float translationSpeed = 1.0f;
        [SerializeField] private float rotationSpeed = 1.0f;
        [SerializeField] private float scaleSpeed = 1.0f;
        
        private ReplicaController _replicaController;
        private Transform _target;

        private Vector3 _translationVelocity = Vector3.zero;
        private Vector3 _scaleVelocity = Vector3.zero;
        private Vector3 _rotationVelocity = Vector3.zero;
        
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
            
//            var translation = translationT * translationSpeed * Time.deltaTime;
//            var translationDirection = (targetPosition - position).normalized;
//            position += translationDirection * translation;
//            
//            var scale = scaleT * scaleSpeed * Time.deltaTime;
//            var scaleDirection = (targetLocalScale - localScale).normalized;
//            localScale += scaleDirection * scale;
//            
//            var rotationAmount = rotationT * rotationSpeed * Time.deltaTime;
//            rotation = Quaternion.RotateTowards(rotation, targetRotation, rotationAmount);
            
            replicaTransform.position = Vector3.SmoothDamp(position, targetPosition, ref _translationVelocity, translationSpeed);
            replicaTransform.localScale = Vector3.SmoothDamp(localScale, targetLocalScale, ref _scaleVelocity, scaleSpeed);

            replicaTransform.rotation = Quaternion.Euler(
                Mathf.SmoothDampAngle(rotation.eulerAngles.x, targetRotation.eulerAngles.x, ref _rotationVelocity.x,
                    rotationSpeed),
                Mathf.SmoothDampAngle(rotation.eulerAngles.y, targetRotation.eulerAngles.y, ref _rotationVelocity.y,
                    rotationSpeed),
                Mathf.SmoothDampAngle(rotation.eulerAngles.z, targetRotation.eulerAngles.z, ref _rotationVelocity.z,
                    rotationSpeed)
            );
        }
        
        public void SetTarget(Transform target)
        {
            _target = target;
        }
        
        public Transform GetTarget()
        {
            return _target;
        }
    }
}
