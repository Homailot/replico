using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Replica;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Serialization;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using InputDevice = UnityEngine.XR.InputDevice;

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
        [SerializeField] private Transform tracker;
        
        [Header("Prefab References")]
        [SerializeField] private GameObject touchPlanePrefab;
        public GestureDetector gestureDetector;

        private NetworkList<Vector3> _pointsOfInterest;
        
        private XROrigin _xrOrigin;
        private readonly NetworkVariable<ulong> _playerId = new NetworkVariable<ulong>();
        private bool _inTable;
        
        public ulong playerId
        {
            get => _playerId.Value;
            set {
                // update gesture detector if is owner
                if (IsOwner && gestureDetector != null)
                {
                    gestureDetector.SetPlayerId(value);
                }
                _playerId.Value = value;  
            }
        }

        private void Awake()
        {
            _xrOrigin = GetComponent<XROrigin>();
            playerTransform.xrOrigin = _xrOrigin;
            _pointsOfInterest = new NetworkList<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
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
            if (!IsOwner) return;

            if (_inTable)
            {
                ChangeSeat(table, seat);
            }
            else
            {
                // delayed to allow for player tracker to correctly update
                StartCoroutine(FirstAttach(table, seat));
            }
        }

        private void ChangeSeat(Table table, int seat)
        { 
            var attachPoint = seat == 0 ? table.firstAttach : table.secondAttach; 
            var trackerToOrigin = playerCamera.transform.position - tracker.position;
            var trackerToOriginTransformed = attachPoint.InverseTransformDirection(trackerToOrigin);
            var position = attachPoint.position;
            playerTransform.SetTransform(position + trackerToOriginTransformed, attachPoint.up, attachPoint.forward);
            var touchPlane = Instantiate(touchPlanePrefab, position, attachPoint.rotation);
            
            var replicaController = touchPlane.GetComponentInChildren<ReplicaController>();
            replicaController.SetObjectToReplicate(GameObject.FindWithTag("ToReplicate"));
            
            gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
            gestureDetector.Init();
            gestureDetector.SetPlayerId(playerId);
            
            foreach (var playerObject in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                foreach (var point in playerObject._pointsOfInterest)
                {
                    gestureDetector.AddPointOfInterest(point, playerObject.playerId);
                }
            } 
            
            gestureDetector.AddPointSelectedListener(OnPointSelected);
        }
        
        private IEnumerator FirstAttach(Table table, int seat)
        {
            yield return new WaitForSeconds(1);
            ChangeSeat(table, seat);
        }

        private void OnPointSelected(Vector3 point)
        {
            if (!IsOwner) return;
            _pointsOfInterest.Add(point);
        }

        private void OnPointsOfInterestChanged(NetworkListEvent<Vector3> changeEvent)
        {
            if (IsOwner) return;
            var player = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            var playerGestureDetector = player.gestureDetector;
            
            switch (changeEvent.Type)
            {
                case NetworkListEvent<Vector3>.EventType.Add:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.AddPointOfInterest(changeEvent.Value, playerId);
                    }
                    break;
                case NetworkListEvent<Vector3>.EventType.Insert:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.AddPointOfInterest(changeEvent.Value, playerId);
                    }
                    break;
                case NetworkListEvent<Vector3>.EventType.Remove:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.RemovePointOfInterest(changeEvent.PreviousValue);
                    }
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
