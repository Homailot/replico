using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Gestures
{
    [Serializable]
    public class GestureConfiguration
    {
        public Vector2 swipeThreshold;
        public float swipeHalfThreshold;
        public float swipeGestureTimeDetection;
        public int swipeFingers;
        [FormerlySerializedAs("replica")] public Replica.ReplicaController replicaController;
        
        public float translateSpeed;
        public Transform movementTarget;
    }
}