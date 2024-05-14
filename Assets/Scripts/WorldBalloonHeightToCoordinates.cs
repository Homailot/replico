using System;
using UnityEngine;

public class WorldBalloonHeightToCoordinates : MonoBehaviour
{
    private Renderer _renderer;
        
    private static readonly int SecondHand = Shader.PropertyToID("_Second_Hand");

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        if (_renderer == null)
        {
            _renderer = GetComponent<Renderer>();
        }
        
        ResetBalloonHeight();
    }

    public void SetBalloonHeight(float height)
    {
        if (_renderer == null) return;
        var bounds = _renderer.bounds;
        var min = bounds.min.y;
        var max = bounds.max.y;
        var remapped = Mathf.Clamp01(Mathf.InverseLerp(min, max, height));
        
        _renderer.material.SetVector(SecondHand, new Vector4(0.5f, remapped, 0, 0));
    }
    
    public void ResetBalloonHeight()
    {
        if (_renderer == null) return;
        _renderer.material.SetVector(SecondHand, new Vector4(0.5f, 0, 0, 0));
    }
}