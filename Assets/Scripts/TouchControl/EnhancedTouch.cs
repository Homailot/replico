using System;
using System.Collections.Generic;

namespace TouchControl
{
    public class EnhancedTouch : TouchFacade
    {
        private List<Finger> _activeFingers = new List<Finger>();

        private void Update()
        {
            if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
            {
                _activeFingers.Clear();
                foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                {
                    _activeFingers.Add(new Finger
                    {
                        Index = touch.finger.index,
                        ScreenPosition = touch.screenPosition
                    });
                }
            }  
        }

        public override List<Finger> GetActiveFingers()
        {
            throw new NotImplementedException();
        }
    }
}