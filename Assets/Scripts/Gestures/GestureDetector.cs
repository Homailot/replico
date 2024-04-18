using System.Collections.Generic;
using System.Linq;
using Gestures.ReplicaTransform;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Serialization;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gestures
{
    public class GestureDetector : MonoBehaviour
    {
        private IGestureState _currentState;
        [SerializeField] private GestureConfiguration gestureConfiguration;
        [SerializeField] private Renderer effectRenderer;
        [SerializeField] private Renderer balloonPlaneRenderer;
        
        private static readonly int ActivationTime = Shader.PropertyToID("_ActivationTime");
        private static readonly int FirstHand = Shader.PropertyToID("_First_Hand");
        private static readonly int SecondHand = Shader.PropertyToID("_Second_Hand");

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            _currentState = new InitialGesture(this, gestureConfiguration);
            _currentState.OnEnter();
        }

        private void Start()
        {
            gestureConfiguration.replicaController.CompleteAnimation(() =>
                {
                    gestureConfiguration.replicaController.ResetTransforms();
                    SwitchState(new TransformReplicaInitialState(this, gestureConfiguration));
                }
            );
        }

        private void Update()
        {
            _currentState.OnUpdate();
        }

        public void OnGestureDetected()
        {
            if (effectRenderer == null) return;
            effectRenderer.material.SetFloat(ActivationTime, Time.time);
        }

        public void UpdateBalloonPlanePositions(Vector2 firstHand, Vector2 secondHand)
        {
            if (balloonPlaneRenderer == null) return;
            var screenMax = Mathf.Max(Screen.width, Screen.height);
            balloonPlaneRenderer.material.SetVector(FirstHand, firstHand / screenMax);
            balloonPlaneRenderer.material.SetVector(SecondHand, secondHand / screenMax);
        }
        
        public void ResetBalloonPlanePositions()
        {
            if (balloonPlaneRenderer == null) return;
            balloonPlaneRenderer.material.SetVector(FirstHand, new Vector2(-1f, -1f));
            balloonPlaneRenderer.material.SetVector(SecondHand, new Vector2(-1f, -1f)); 
        }
        
        public void SwitchState(IGestureState newState)
        {
            _currentState.OnExit();
            _currentState = newState;
            _currentState.OnEnter();
        }
    }
}
