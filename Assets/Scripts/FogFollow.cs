using UnityEngine;

public class FogFollow : MonoBehaviour
{
    private Camera _playerCamera;
    
    private void Start()
    {
        _playerCamera = Camera.main;
    }
    
    private void Update()
    {
        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
            return;
        }
        
        transform.position = _playerCamera.transform.position;
    }
}