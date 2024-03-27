using System;
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
    
    private int _writeWidth;
    private int _writeHeight;
    
    private Material _material;
    private RenderTexture _fingerHistory;
    private ComputeBuffer _fingerPositionsBuffer;
    private ComputeBuffer _lastFingerPositionsBuffer;
    private ComputeBuffer _averageInclinationBuffer;
    private float4[] _currentFingerPositions;
    private float4[] _lastFingerPositions;
    private float[] _averageInclination;
    
    private const int ShaderGroupSize = 8;
    private static readonly int ComputeFingerHistory = Shader.PropertyToID("finger_history");
    private static readonly int FingerPositions = Shader.PropertyToID("finger_positions");
    private static readonly int LinearDecayRate = Shader.PropertyToID("linear_decay_rate");
    private static readonly int QuadraticDecayRate = Shader.PropertyToID("quadratic_decay_rate");
    private static readonly int DeltaTime = Shader.PropertyToID("delta_time");
    private static readonly int LastFingerPositions = Shader.PropertyToID("last_finger_positions");
    private static readonly int AverageIncline = Shader.PropertyToID("average_incline");

    private HashSet<Finger> _fingers;
    private static readonly int FingerRadius = Shader.PropertyToID("finger_radius");
    private static readonly int FingerHistory = Shader.PropertyToID("_Finger_History");

    private void Awake()
    {
        EnhancedTouchSupport.Enable(); 
    }

    private void Start()
    {
        var width = Mathf.NextPowerOfTwo(Screen.width);
        var height = Mathf.NextPowerOfTwo(Screen.height);
        var textureSize = Mathf.Max(width, height);
        
        _writeWidth = Screen.width;
        _writeHeight = Screen.height;
        
        _fingerHistory = new RenderTexture(textureSize,
            textureSize,
            0,
            RenderTextureFormat.ARGBFloat,
            RenderTextureReadWrite.Linear)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            autoGenerateMips = false,
            enableRandomWrite = true
        };
        _fingerHistory.Create();
        
        var mesh = GetComponent<MeshFilter>().mesh;
        // Update the UVs of the mesh to match the aspect ratio of the screen
        var uvs = new Vector2[mesh.uv.Length];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(0, (float) Screen.height / _fingerHistory.height);
        uvs[2] = new Vector2((float) Screen.width / _fingerHistory.width, (float) Screen.height / _fingerHistory.height);
        uvs[3] = new Vector2((float) Screen.width / _fingerHistory.width, 0);
        mesh.uv = uvs;
        
        var currentActive = RenderTexture.active;
        RenderTexture.active = _fingerHistory;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = currentActive;
        
        _averageInclination = new float[2];
        _currentFingerPositions = new float4[2];
        _lastFingerPositions = new float4[2];
        _averageInclinationBuffer = new ComputeBuffer(2, sizeof(float));
        _fingerPositionsBuffer = new ComputeBuffer(2, sizeof(float) * 4);
        _lastFingerPositionsBuffer = new ComputeBuffer(2, sizeof(float) * 4);
        
        Array.Fill(_averageInclination, 0);
        
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
        decayFingerHistory.SetBuffer(0, AverageIncline, _averageInclinationBuffer);
        decayFingerHistory.SetFloat(LinearDecayRate, fingerDecay);
        decayFingerHistory.SetFloat(QuadraticDecayRate, fingerQuadraticDecay);
        decayFingerHistory.SetFloat(FingerRadius, fingerRadius);
        
        _fingers = new HashSet<Finger>();
    }

    private void OnDestroy()
    {
        _fingerPositionsBuffer.Release();
        _lastFingerPositionsBuffer.Release();
        _averageInclinationBuffer.Release();
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
                new float2(_writeWidth, _writeHeight)); 
            newPosition = math.clamp(newPosition, float2.zero, new float2(_writeWidth, _writeHeight));
            
            if (_fingers.Add(finger))
            {
                UpdateFingerPosition(ref _lastFingerPositions, finger.index, newPosition);
            }
            else
            {
                UpdateFingerPosition(ref _lastFingerPositions, finger.index, GetFingerPosition(ref _currentFingerPositions, finger.index));
            }
            
            UpdateFingerPosition(ref _currentFingerPositions, finger.index, newPosition);
            
            if (newPosition.Equals(GetFingerPosition(ref _lastFingerPositions, finger.index)))
            {
                continue;
            }
            
            var currentInclination = math.atan2(newPosition.y - GetFingerPosition(ref _lastFingerPositions, finger.index).y,
                newPosition.x - GetFingerPosition(ref _lastFingerPositions, finger.index).x);
            _averageInclination[finger.index] = Mathf.LerpAngle(_averageInclination[finger.index] * Mathf.Rad2Deg, currentInclination * Mathf.Rad2Deg, 0.5f) * Mathf.Deg2Rad;
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
        _averageInclinationBuffer.SetData(_averageInclination);
        
        decayFingerHistory.SetFloat(DeltaTime, Time.deltaTime);
        decayFingerHistory.Dispatch(0,
            _fingerHistory.width / ShaderGroupSize,
            _fingerHistory.height / ShaderGroupSize,
            1);
    }
}
