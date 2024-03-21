using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchDraw : MonoBehaviour
{
    [SerializeField] private float fingerDecay = 0.9f;
    [SerializeField] private float fingerQuadraticDecay = 0.0f;
    [SerializeField] private float fingerRadius = 0.01f;
    
    [SerializeField] private ComputeShader decayFingerHistory;
    
    private Material _material;
    private RenderTexture _fingerHistory;
    private ComputeBuffer _fingerPositionsBuffer;
    private uint4[] _currentFingerPositions;

    private static readonly int ShaderGroupSize = 8;
    private static readonly int FingerHistory = Shader.PropertyToID("_Finger_History");
    private static readonly int ComputeFingerHistory = Shader.PropertyToID("finger_history");
    private static readonly int FingerPositions = Shader.PropertyToID("finger_positions");
    private static readonly int LinearDecayRate = Shader.PropertyToID("linear_decay_rate");
    private static readonly int QuadraticDecayRate = Shader.PropertyToID("quadratic_decay_rate");
    private static readonly int DeltaTime = Shader.PropertyToID("delta_time");

    private void Awake()
    {
        EnhancedTouchSupport.Enable(); 
    }

    private void Start()
    {
        _fingerHistory = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        _fingerHistory.Create();
        
        var currentActive = RenderTexture.active;
        RenderTexture.active = _fingerHistory;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = currentActive;
        
        _currentFingerPositions = new uint4[2];
        _fingerPositionsBuffer = new ComputeBuffer(2, sizeof(uint) * 4);
        
        _material = GetComponent<Renderer>().material; 
        _material.SetTexture(FingerHistory, _fingerHistory);
        
        decayFingerHistory.SetTexture(0, ComputeFingerHistory, _fingerHistory);
        decayFingerHistory.SetBuffer(0, FingerPositions, _fingerPositionsBuffer);
        decayFingerHistory.SetFloat(LinearDecayRate, fingerDecay);
        decayFingerHistory.SetFloat(QuadraticDecayRate, fingerQuadraticDecay);
    }

    private void Update()
    {
        decayFingerHistory.SetFloat(DeltaTime, Time.deltaTime);
        foreach (var finger in Touch.activeFingers)
        {
            Debug.Log(finger.screenPosition);
            var screenPosition = finger.screenPosition;
            var fingerPosition = new uint4((uint)screenPosition.x, (uint)screenPosition.y, 0, 0);
            _currentFingerPositions[finger.index] = fingerPosition;
        }
        
        _fingerPositionsBuffer.SetData(_currentFingerPositions);
        decayFingerHistory.Dispatch(0, _fingerHistory.width / 8, _fingerHistory.height / 8, 1);
    }
}
