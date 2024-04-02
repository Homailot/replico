using System.Collections.Generic;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures
{
    public interface IGestureState
    {
        void OnEnter()
        {
            
        }

        void OnExit()
        {
            
        }
        
        void OnUpdate();
    }
}