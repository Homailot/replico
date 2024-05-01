using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public class PlayerTransform : NetworkTransform
    {
        public XROrigin xrOrigin;

        public void SetTransform(Transform playerCamera, Transform attachPoint, Transform tracker)
        {
            var trackerInverseRotation = tracker.rotation * Quaternion.Euler(0, 180, 0);
            var deltaAngle = Quaternion.Inverse(trackerInverseRotation) * attachPoint.rotation;
            var deltaAngleY = Quaternion.Euler(0, deltaAngle.eulerAngles.y, 0);
            var playerRotationY = Quaternion.Euler(0, playerCamera.rotation.eulerAngles.y, 0);
            var targetRotation = playerRotationY * deltaAngleY;
            var targetForward = targetRotation * Vector3.forward;
            xrOrigin.MatchOriginUpCameraForward(attachPoint.up, targetForward);
            
            var trackerToOrigin = playerCamera.position - tracker.position;
            Debug.Log($"Tracker to origin: {trackerToOrigin}");
            //var trackerToOriginTransformed = attachPoint.InverseTransformDirection(trackerToOrigin);
            //Debug.Log($"Tracker to origin transformed: {trackerToOriginTransformed}");
            var position = attachPoint.position;
            Debug.Log($"Position: {position}");
            xrOrigin.MoveCameraToWorldLocation(position + trackerToOrigin);
            Debug.Log($"Player transform position: {transform.position}");
        }
        
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}