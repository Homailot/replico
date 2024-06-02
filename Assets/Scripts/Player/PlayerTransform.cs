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
        private static Quaternion _trackerRotation;

        public void SetTransform(Transform player, Transform playerCamera, Transform attachPoint, Transform tracker)
        {
            Vector3 trackerPosition;
            Quaternion trackerRotation;
            if (!_initialized)
            {
                 _trackerRotation = tracker.rotation * Quaternion.Inverse(player.rotation);
                 trackerRotation = tracker.rotation;
            }
            else
            {
                trackerRotation = player.rotation * _trackerRotation;
            }
            
            var trackerInverseRotation = trackerRotation * Quaternion.Euler(0, 180, 0);
            var deltaAngle = Quaternion.Inverse(trackerInverseRotation) * attachPoint.rotation;
            var deltaAngleY = Quaternion.Euler(0, deltaAngle.eulerAngles.y, 0);
            var playerRotationY = Quaternion.Euler(0, playerCamera.rotation.eulerAngles.y, 0);
            var targetRotation = playerRotationY * deltaAngleY;
            var targetForward = targetRotation * Vector3.forward;
            xrOrigin.MatchOriginUpCameraForward(attachPoint.up, targetForward);
            
            if (!_initialized)
            {
                _trackerLocal = player.InverseTransformPoint(tracker.position);
                trackerPosition = tracker.position;
                _initialized = true;
            }
            else
            {
                trackerPosition = player.TransformPoint(_trackerLocal); 
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