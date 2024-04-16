using System.Collections.Generic;
using UnityEngine;

namespace TouchControl
{
    public abstract class TouchFacade : MonoBehaviour
    {
        public abstract List<Finger> GetActiveFingers(); 
    }
}