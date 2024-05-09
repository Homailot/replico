using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera _camera;
    
    private void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        } 
        
        transform.rotation = _camera.transform.rotation;
    }
}