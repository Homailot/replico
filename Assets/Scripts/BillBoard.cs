using UnityEngine;

public class BillBoard : MonoBehaviour
{
    [SerializeField] private new Camera camera;

    private void LateUpdate()
    {
        if (camera == null) return;

        var target = camera.transform.position;
        target.y = transform.position.y;
        transform.LookAt(target);
    } 
}