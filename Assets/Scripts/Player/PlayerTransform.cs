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
        
        private static bool _initialized;
        private static Vector3 _trackerLocal;

        public void SetTransform(Transform playerCamera, Transform attachPoint, Transform tracker)
        {
            var trackerInverseRotation = tracker.rotation * Quaternion.Euler(0, 180, 0);
            var deltaAngle = Quaternion.Inverse(trackerInverseRotation) * attachPoint.rotation;
            var deltaAngleY = Quaternion.Euler(0, deltaAngle.eulerAngles.y, 0);
            var playerRotationY = Quaternion.Euler(0, playerCamera.rotation.eulerAngles.y, 0);
            var targetRotation = playerRotationY * deltaAngleY;
            var targetForward = targetRotation * Vector3.forward;
            xrOrigin.MatchOriginUpCameraForward(attachPoint.up, targetForward);

            Vector3 trackerPosition;
            if (!_initialized)
            {
                _trackerLocal = tracker.localPosition;
                trackerPosition = tracker.position;
                _initialized = true;
            }
            else
            {
                Debug.Log("Using tracker local position.");
                trackerPosition = tracker.parent.TransformPoint(_trackerLocal);
            }
            
            var trackerToOrigin = playerCamera.position - trackerPosition;
            var position = attachPoint.position;
            xrOrigin.MoveCameraToWorldLocation(position + trackerToOrigin);
        }
        
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}