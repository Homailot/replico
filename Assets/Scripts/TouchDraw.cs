using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private ComputeBuffer _lastFingerPositionsBuffer;
    private float4[] _currentFingerPositions;
    private float4[] _lastFingerPositions;

    private const int ShaderGroupSize = 8;
    private static readonly int ComputeFingerHistory = Shader.PropertyToID("finger_history");
    private static readonly int FingerPositions = Shader.PropertyToID("finger_positions");
    private static readonly int LinearDecayRate = Shader.PropertyToID("linear_decay_rate");
    private static readonly int QuadraticDecayRate = Shader.PropertyToID("quadratic_decay_rate");
    private static readonly int DeltaTime = Shader.PropertyToID("delta_time");
    private static readonly int LastFingerPositions = Shader.PropertyToID("last_finger_positions");

    private HashSet<Finger> _fingers;
    private static readonly int FingerRadius = Shader.PropertyToID("finger_radius");
    private static readonly int FingerHistory = Shader.PropertyToID("_Finger_History");

    private void Awake()
    {
        EnhancedTouchSupport.Enable(); 
    }

    private void Start()
    {
        _fingerHistory = new RenderTexture(Screen.width,
            Screen.height,
            0,
            RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Linear)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            enableRandomWrite = true
        };
        _fingerHistory.Create();
        
        var currentActive = RenderTexture.active;
        RenderTexture.active = _fingerHistory;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = currentActive;
        
        _currentFingerPositions = new float4[2];
        _lastFingerPositions = new float4[2];
        _fingerPositionsBuffer = new ComputeBuffer(2, sizeof(float) * 8);
        _lastFingerPositionsBuffer = new ComputeBuffer(2, sizeof(float) * 8);
        
        Array.Fill(_currentFingerPositions,
            new float4(
                (_fingerHistory.width + 1),
                (_fingerHistory.height + 1),
                (_fingerHistory.width + 1),
                (_fingerHistory.height + 1)
            )
        );
        Array.Fill(_lastFingerPositions,
            new float4(
                (_fingerHistory.width + 1),
                (_fingerHistory.height + 1),
                (_fingerHistory.width + 1),
                (_fingerHistory.height + 1)
            )
        );
        
        _material = GetComponent<Renderer>().material;
        _material.SetTexture(FingerHistory, _fingerHistory);
        
        decayFingerHistory.SetTexture(0, ComputeFingerHistory, _fingerHistory);
        decayFingerHistory.SetBuffer(0, FingerPositions, _fingerPositionsBuffer);
        decayFingerHistory.SetBuffer(0, LastFingerPositions, _lastFingerPositionsBuffer);
        decayFingerHistory.SetFloat(LinearDecayRate, fingerDecay);
        decayFingerHistory.SetFloat(QuadraticDecayRate, fingerQuadraticDecay);
        decayFingerHistory.SetFloat(FingerRadius, fingerRadius);
        
        _fingers = new HashSet<Finger>();
    }

    private void OnDestroy()
    {
        _fingerPositionsBuffer.Release();
        _lastFingerPositionsBuffer.Release();
        Destroy(_fingerHistory);
    }

    private void OnValidate()
    {
        decayFingerHistory.SetFloat(LinearDecayRate, fingerDecay);
        decayFingerHistory.SetFloat(QuadraticDecayRate, fingerQuadraticDecay);
        decayFingerHistory.SetFloat(FingerRadius, fingerRadius);
    }

    private static void UpdateFingerPosition(ref float4[] fingerPositions, int index, float2 newPosition)
    {
        var innerIndex = index % 2;
        var outerIndex = index / 2;

        if (innerIndex == 0)
        {
            fingerPositions[outerIndex].xy = newPosition;
        }
        else
        {
            fingerPositions[outerIndex].zw = newPosition; 
        }
    }
    
    private static float2 GetFingerPosition(ref float4[] fingerPositions, int index)
    {
        var innerIndex = index % 2;
        var outerIndex = index / 2;

        return innerIndex == 0 ? fingerPositions[outerIndex].xy : fingerPositions[outerIndex].zw;
    }
    
    private static float2 Remap(float2 value, float2 low1, float2 high1, float2 low2, float2 high2)
    {
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1); 
    }

    private void Update()
    {
        foreach (var finger in Touch.activeFingers)
        {
            var screenPosition = finger.screenPosition;
            var newPosition = new float2(screenPosition.x, screenPosition.y);
            newPosition = Remap(newPosition,
                float2.zero,
                new float2(Screen.width, Screen.height),
                float2.zero,
                new float2(_fingerHistory.width, _fingerHistory.height)); 
            newPosition = math.clamp(newPosition, float2.zero, new float2(_fingerHistory.width, _fingerHistory.height));
            
            if (_fingers.Add(finger))
            {
                UpdateFingerPosition(ref _lastFingerPositions, finger.index, newPosition);
            }
            else
            {
                UpdateFingerPosition(ref _lastFingerPositions, finger.index, GetFingerPosition(ref _currentFingerPositions, finger.index));
            }
            
            UpdateFingerPosition(ref _currentFingerPositions, finger.index, newPosition);
        }

        var resetPosition = new float2(_fingerHistory.width + 1, _fingerHistory.height + 1);
        foreach (var finger in _fingers.Where(finger => !finger.isActive))
        {
            UpdateFingerPosition(ref _lastFingerPositions, finger.index, resetPosition);
            UpdateFingerPosition(ref _currentFingerPositions, finger.index, resetPosition);
        }
        _fingers.RemoveWhere(finger => !finger.isActive);
        
        _fingerPositionsBuffer.SetData(_currentFingerPositions);
        _lastFingerPositionsBuffer.SetData(_lastFingerPositions);
        
        decayFingerHistory.SetFloat(DeltaTime, Time.deltaTime);
        decayFingerHistory.Dispatch(0,
            _fingerHistory.width / ShaderGroupSize,
            _fingerHistory.height / ShaderGroupSize,
            1);
    }
}
