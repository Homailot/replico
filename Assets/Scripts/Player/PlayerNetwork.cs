using System;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Player
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private TrackedPoseDriver trackedPoseDriver;
        [SerializeField] private GameObject playerModel;
        [SerializeField] private GameObject rightController;
        [SerializeField] private GameObject leftController;
        [SerializeField] private PlayerTransform playerTransform;

        private void Awake()
        {
            playerTransform.xrOrigin = GetComponent<XROrigin>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            var xrOrigin = GetComponent<XROrigin>();
            var characterController = GetComponent<CharacterController>();
            var characterControllerDriver = GetComponent<CharacterControllerDriver>();
            var xrInputModalityManager = GetComponent<XRInputModalityManager>();
            
            if (IsOwner)
            {
                playerCamera.enabled = true;
                audioListener.enabled = true;
                trackedPoseDriver.enabled = true;
                rightController.SetActive(true);
                leftController.SetActive(true);
                xrOrigin.enabled = true;
                characterController.enabled = true;
                characterControllerDriver.enabled = true;
                xrInputModalityManager.enabled = true;
            }
            else
            {
                playerModel.SetActive(true);
            }
        }
        
        public void MovePlayerToTable(Table table, int seat)
        {
            switch (seat)
            {
                case 0:
                {
                    var firstAttach = table.firstSeatAttach;
                    playerTransform.SetTransform(firstAttach.position, firstAttach.up, firstAttach.forward);
                    break;
                }
                case 1:
                {
                    var secondAttach = table.secondSeatAttach;
                    playerTransform.SetTransform(secondAttach.position, secondAttach.up, secondAttach.forward);
                    break;
                }
            }
            
            // TODO: create local touch plane
        }
    }
}
