using System.Collections.Generic;
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
        
        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            _currentState = new InitialGesture(this, gestureConfiguration);
            _currentState.OnEnter();
        }
    
        private void Update()
        {
            _currentState.OnUpdate();
        }
        
        public void SwitchState(IGestureState newState)
        {
            _currentState.OnExit();
            _currentState = newState;
            _currentState.OnEnter();
        }
    }
}
