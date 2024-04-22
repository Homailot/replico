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
        [SerializeField] private Transform balloon;
        
        private static readonly int ActivationTime = Shader.PropertyToID("_ActivationTime");
        private static readonly int FirstHand = Shader.PropertyToID("_First_Hand");
        private static readonly int SecondHand = Shader.PropertyToID("_Second_Hand");
        private static readonly int Disabled = Shader.PropertyToID("_Disabled");

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            _currentState = new InitialGesture(this, gestureConfiguration);
            _currentState.OnEnter();
        }

        private void Start()
        {
            DisableBalloon(); 
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

        public void EnableBalloon()
        {
            if (balloon == null) return;
            balloon.gameObject.SetActive(true);
        }
        
        public void DisableBalloon()
        {
            if (balloon == null) return;
            balloon.gameObject.SetActive(false);
        }
        
        public void UpdateBalloonPosition(Vector3 position)
        {
            if (balloon == null) return;
            var screenPosition = new Vector2(position.x, position.z);
            balloon.position = gestureConfiguration.touchToPosition.GetTouchPosition(screenPosition);
            
            balloon.position = new Vector3(balloon.position.x, balloon.position.y + position.y, balloon.position.z);
        }
        
        public void ToggleBalloonPlaneLine(bool active)
        {
            if (balloonPlaneRenderer == null) return;
            balloonPlaneRenderer.material.SetInt(Disabled, active ? 0 : 1);
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
