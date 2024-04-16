using System.Collections.Generic;

namespace TouchControl
{
    public class TouchSimulator : TouchFacade
    {
        private List<Finger> _activeFingers = new List<Finger>();
    
        private void Update()
        {
        }

        public override List<Finger> GetActiveFingers()
        {
            throw new System.NotImplementedException();
        }
    }
}