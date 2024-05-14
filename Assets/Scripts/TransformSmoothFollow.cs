using UnityEngine;

public class TransformSmoothFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.3f;
    
    private Vector3 _velocity = Vector3.zero;
    
    private void Update()
    {
        if (target == null)
        {
            return;
        }
        
        var targetPosition = target.position;
        var position = transform.position;
        
        transform.position = Vector3.SmoothDamp(position, targetPosition, ref _velocity, smoothTime);
    }

    public void UpdateTarget(Vector3 position)
    {
        target.position = position;
    }
}