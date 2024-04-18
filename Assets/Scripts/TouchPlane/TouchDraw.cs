using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Utils;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace TouchPlane
{
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
        private RenderTexture _finger2History;
        private RenderTexture _finger3History;
        private RenderTexture _finger4History;
        private RenderTexture _finger5History;
        private ComputeBuffer _fingerPositionsBuffer;
        private ComputeBuffer _lastFingerPositionsBuffer;
        private ComputeBuffer _averageInclinationBuffer;
        private float4[] _currentFingerPositions;
        private float4[] _lastFingerPositions;
        private float[] _averageInclination;
    
        private const int ShaderGroupSize = 8;
        private static readonly int ComputeFingerHistory = Shader.PropertyToID("finger_history");
        private static readonly int ComputeFinger2History = Shader.PropertyToID("finger_history_2");
        private static readonly int ComputeFinger3History = Shader.PropertyToID("finger_history_3");
        private static readonly int ComputeFinger4History = Shader.PropertyToID("finger_history_4");
        private static readonly int ComputeFinger5History = Shader.PropertyToID("finger_history_5");
        private static readonly int FingerPositions = Shader.PropertyToID("finger_positions");
        private static readonly int LinearDecayRate = Shader.PropertyToID("linear_decay_rate");
        private static readonly int QuadraticDecayRate = Shader.PropertyToID("quadratic_decay_rate");
        private static readonly int DeltaTime = Shader.PropertyToID("delta_time");
        private static readonly int LastFingerPositions = Shader.PropertyToID("last_finger_positions");
        private static readonly int AverageIncline = Shader.PropertyToID("average_incline");

        private HashSet<Finger> _fingers;
        private static readonly int FingerRadius = Shader.PropertyToID("finger_radius");
        private static readonly int FingerHistory = Shader.PropertyToID("_Finger_History");
        private static readonly int Finger2History = Shader.PropertyToID("_Finger_History_2");
        private static readonly int Finger3History = Shader.PropertyToID("_Finger_History_3");
        private static readonly int Finger4History = Shader.PropertyToID("_Finger_History_4");
        private static readonly int Finger5History = Shader.PropertyToID("_Finger_History_5");

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

            _fingerHistory = CreateRenderTexture(textureSize, textureSize);
            _finger2History = CreateRenderTexture(textureSize, textureSize);
            _finger3History = CreateRenderTexture(textureSize, textureSize);
            _finger4History = CreateRenderTexture(textureSize, textureSize);
            _finger5History = CreateRenderTexture(textureSize, textureSize);
        
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
        
            _averageInclination = new float[10];
            _currentFingerPositions = new float4[5];
            _lastFingerPositions = new float4[5];
            _averageInclinationBuffer = new ComputeBuffer(10, sizeof(float));
            _fingerPositionsBuffer = new ComputeBuffer(5, sizeof(float) * 4);
            _lastFingerPositionsBuffer = new ComputeBuffer(5, sizeof(float) * 4);
        
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
            _material.SetTexture(Finger2History, _finger2History);
            _material.SetTexture(Finger3History, _finger3History);
            _material.SetTexture(Finger4History, _finger4History);
            _material.SetTexture(Finger5History, _finger5History);
        
            decayFingerHistory.SetTexture(0, ComputeFingerHistory, _fingerHistory);
            decayFingerHistory.SetTexture(0, ComputeFinger2History, _finger2History);
            decayFingerHistory.SetTexture(0, ComputeFinger3History, _finger3History);
            decayFingerHistory.SetTexture(0, ComputeFinger4History, _finger4History);
            decayFingerHistory.SetTexture(0, ComputeFinger5History, _finger5History);
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

        private static RenderTexture CreateRenderTexture(int width, int height)
        {
            var texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                enableRandomWrite = true
            };
            texture.Create();
            return texture;
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

        private void Update()
        {
            foreach (var finger in Touch.activeFingers)
            {
                var screenPosition = finger.screenPosition;
                var newPosition = new float2(screenPosition.x, screenPosition.y);
                newPosition = MathUtils.Remap(newPosition,
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
                5);
        }
    }
}
