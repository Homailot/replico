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
        public TouchToPosition touchToPosition;
        
        public float translateSpeed;
        public float scaleSpeed;
        public Transform movementTarget;
        
        public float clusterEps;
        public int clusterMinPts;
    }
}