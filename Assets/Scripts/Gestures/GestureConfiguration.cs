using System;
using TouchPlane;
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
        public Logger logger;
        public TouchToPosition touchToPosition;
        public Transform frame;
        
        public float translateSpeed;
        public float scaleSpeed;
        public Transform movementTarget;
        
        public float handDistanceThreshold;
        public float handMovementDetectionDistance;
        public float handMovementDetectionTime;
        public float verticalGestureHandEmptyAllowance;
        
        public float balloonDistanceMultiplier;
        public float balloonMovementDetectionDistance;
        public float balloonTeleportTime;
        public float balloonRotationSpeed;
        public float balloonShowArrowTime;
        public float balloonSelectionDistanceThreshold;
    }
}