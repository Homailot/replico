using System;
using UnityEngine;

namespace Gestures
{
    [Serializable]
    public class GestureConfiguration
    {
        public Vector2 swipeThreshold;
        public int swipeFingers;
        public float swipeHalfThreshold;
        public Replica replica;
    }
}