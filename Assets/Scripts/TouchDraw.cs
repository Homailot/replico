using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchDraw : MonoBehaviour
{
    private Material _material;
    private static readonly int Property = Shader.PropertyToID("_Finger_1_Position");

    private void Awake()
    {
        EnhancedTouchSupport.Enable(); 
    }

    private void Start()
    {
        _material = GetComponent<Renderer>().material; 
    }

    private void Update()
    {
        Debug.Log(Screen.width);
        if (Touch.activeTouches.Count == 0)
        {
            _material.SetVector(Property, new Vector4(-1000, -1000));
            return;
        }
        
        foreach (var finger in Touch.activeFingers)
        {
            Debug.Log(finger.screenPosition);
            _material.SetVector(Property, new Vector4(finger.screenPosition.x, finger.screenPosition.y));
        } 
    }
}
