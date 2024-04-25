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

        public void SetTransform(Vector3 position, Vector3 up, Vector3 forward)
        {
            xrOrigin.MoveCameraToWorldLocation(position);
            xrOrigin.MatchOriginUpCameraForward(up, forward);
        }
        
        
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}