using System;
using UnityEngine;
using UnityEngine.Serialization;

public class BalloonScale : MonoBehaviour
{
    private Camera _camera;
    
    public float initialScale;
    public float scaleMultiplier = 1.5f;
    public float maxScale = 2.0f;
    
    private void Start()
    {
        _camera = Camera.main;
    }

    public void Update()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
            return;
        } 
        
        var distance = Vector3.Distance(transform.position, _camera.transform.position);
        var scale = Mathf.Min(initialScale + distance * scaleMultiplier, maxScale);
        
        transform.localScale = new Vector3(scale, scale, scale);
    }
}