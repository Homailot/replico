using System;
using System.Collections.Generic;
using Gestures;
using Replica;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace Player
{
    public class PlayerNetwork : NetworkBehaviour
    {
        [Header("Local Player Components")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private TrackedPoseDriver trackedPoseDriver;
        [SerializeField] private GameObject playerModel;
        [SerializeField] private GameObject rightController;
        [SerializeField] private GameObject leftController;
        [SerializeField] private PlayerTransform playerTransform;
        
        [Header("Prefab References")]
        [SerializeField] private GameObject touchPlanePrefab;

        private readonly NetworkList<Vector3> _pointsOfInterest =
            new NetworkList<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
        public GestureDetector gestureDetector;

        private void Awake()
        {
            playerTransform.xrOrigin = GetComponent<XROrigin>();
        }

        private void Start()
        {
            _pointsOfInterest.OnListChanged += OnPointsOfInterestChanged;
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
                    
                    var trackerAttach = table.firstAttach;
                    var touchPlane = Instantiate(touchPlanePrefab, trackerAttach.position, trackerAttach.rotation);
                    var replicaController = touchPlane.GetComponentInChildren<ReplicaController>();
                    replicaController.SetObjectToReplicate(GameObject.FindWithTag("ToReplicate"));
                    gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
                    gestureDetector.Init();
                    // todo: add all points of interest to the gesture detector
                    gestureDetector.AddPointSelectedListener(OnPointSelected);
                    break;
                }
                case 1:
                {
                    var secondAttach = table.secondSeatAttach;
                    playerTransform.SetTransform(secondAttach.position, secondAttach.up, secondAttach.forward);
                    
                    var trackerAttach = table.secondAttach;
                    var touchPlane = Instantiate(touchPlanePrefab, trackerAttach.position, trackerAttach.rotation);
                    var replicaController = touchPlane.GetComponentInChildren<ReplicaController>();
                    replicaController.SetObjectToReplicate(GameObject.FindWithTag("ToReplicate"));
                    gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
                    gestureDetector.Init();
                    gestureDetector.AddPointSelectedListener(OnPointSelected);
                    break;
                }
            }
        }

        private void OnPointSelected(Vector3 point)
        {
            Debug.Log("OnPointSelected");
            if (!IsOwner) return;
            Debug.Log($"Point selected: {point}");
            _pointsOfInterest.Add(point);
        }

        private void OnPointsOfInterestChanged(NetworkListEvent<Vector3> changeEvent)
        {
            Debug.Log("OnPointsOfInterestChanged"); 
            if (IsOwner) return;
            Debug.Log("IsOwner");
            Debug.Log(changeEvent.Type);
            Debug.Log(changeEvent.Value);
            
            // todo: implement other cases
            switch (changeEvent.Type)
            {
                case NetworkListEvent<Vector3>.EventType.Add:
                    Debug.Log("Adding");
                    var player = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
                    var playerGestureDetector = player.gestureDetector;
                    if (playerGestureDetector != null)
                    {
                        Debug.Log("Adding");
                        playerGestureDetector.AddPointOfInterest(changeEvent.Value);
                    }
                    break;
                case NetworkListEvent<Vector3>.EventType.Insert:
                    break;
                case NetworkListEvent<Vector3>.EventType.Remove:
                    break;
                case NetworkListEvent<Vector3>.EventType.RemoveAt:
                    break;
                case NetworkListEvent<Vector3>.EventType.Value:
                    break;
                case NetworkListEvent<Vector3>.EventType.Clear:
                    break;
                case NetworkListEvent<Vector3>.EventType.Full:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
