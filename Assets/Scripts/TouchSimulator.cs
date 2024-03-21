using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class TouchSimulator : MonoBehaviour
{
    private void OnEnable()
    {
        TouchSimulation.Enable();
    }
}
