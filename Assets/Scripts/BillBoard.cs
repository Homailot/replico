using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private void LateUpdate()
    {
        var cameraTarget = Camera.main;
        if (cameraTarget == null) return;

        var target = cameraTarget.transform.position;
        target.y = transform.position.y;
        transform.LookAt(target);
    } 
}