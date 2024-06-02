using UnityEngine;

public class VerticalBillboard : MonoBehaviour
{
    [SerializeField] private bool _invert;
    
    private void LateUpdate()
    {
        var cameraTarget = Camera.main;
        if (cameraTarget == null) return;

        var target = cameraTarget.transform.position;
        target.y = transform.position.y;
        transform.LookAt(target);
        
        if (_invert)
        {
            transform.Rotate(0, 180, 0);
        }
    } 
}