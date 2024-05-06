using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Replica;
using Tables;
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
        [SerializeField] private List<GameObject> playerModels;
        public GestureDetector gestureDetector;

        private NetworkList<Vector3> _pointsOfInterest;
        
        private XROrigin _xrOrigin;
        private readonly NetworkVariable<ulong> _playerId = new NetworkVariable<ulong>();
        private bool _initialized;

        private GameObject _touchPlane;
        
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
        
        public PlayerManager playerManager { get; set; }

        private void Awake()
        {
            _xrOrigin = GetComponent<XROrigin>();
            playerTransform.xrOrigin = _xrOrigin;
            _pointsOfInterest = new NetworkList<Vector3>(writePerm: NetworkVariableWritePermission.Owner);
        }

        private void Start()
        {
            _pointsOfInterest.OnListChanged += OnPointsOfInterestChanged;
            playerManager = FindObjectOfType<PlayerManager>();
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
                _playerId.OnValueChanged += OnPlayerIdChanged;
                OnPlayerIdChanged(0, playerId);
            }
        }

        private void OnPlayerIdChanged(ulong previous, ulong newValue)
        {
            SetPlayerModel(playerModels[(int)newValue % playerModels.Count]);
        }

        public void MovePlayerToTable(Table table, int seat)
        {
            if (!IsOwner) return;

            if (_initialized)
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
            var position = attachPoint.position;
            playerTransform.SetTransform(playerCamera.transform, attachPoint, tracker);

            GameObject touchPlane;
            if (_touchPlane != null)
            {
                touchPlane = _touchPlane;
                touchPlane.transform.position = position;
                touchPlane.transform.rotation = attachPoint.rotation;
                return;
            }
            
            touchPlane = Instantiate(touchPlanePrefab, position, attachPoint.rotation);
            
            var objectToReplicate = GameObject.FindWithTag("ToReplicate");
            var world = objectToReplicate.GetComponent<World>();
            
            gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
            gestureDetector.SetWorld(world);
            gestureDetector.Init();
            gestureDetector.SetPlayerId(playerId);
            
            foreach (var playerObject in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                foreach (var point in playerObject._pointsOfInterest)
                {
                    gestureDetector.AddPointOfInterest(point, playerObject.playerId);
                }
            }

            foreach (var tableObject in FindObjectsByType<Table>(FindObjectsSortMode.None))
            {
                gestureDetector.CreateTable(tableObject.NetworkObjectId, tableObject.firstSeat.Value,
                    tableObject.secondSeat.Value,
                    tableObject.transform.position, tableObject.transform.rotation);
            }
            
            gestureDetector.AddPointSelectedListener(OnPointSelected);
            gestureDetector.AddPointRemovedListener(OnPointRemoved);
            gestureDetector.AddTeleportSelectedListener(OnTeleportSelected);
            _touchPlane = touchPlane;
            _initialized = true;
        }
        
        private IEnumerator FirstAttach(Table table, int seat)
        {
            yield return new WaitForSeconds(1);
            ChangeSeat(table, seat);
        }

        private void SetPlayerModel(GameObject model)
        {
            if (playerModel.transform.childCount > 0)
            {
                Destroy(playerModel.transform.GetChild(0).gameObject);
            }

            Instantiate(model, playerModel.transform.position, playerModel.transform.rotation, playerModel.transform);
        }

        private void OnPointSelected(Vector3 point)
        {
            if (!IsOwner) return;
            _pointsOfInterest.Add(point);
        }
        
        private void OnPointRemoved(Vector3 point)
        {
            if (!IsOwner) return;
            _pointsOfInterest.Remove(point);
        }

        private void OnTeleportSelected(Vector3 point, Quaternion rotation)
        {
            if (!IsOwner) return;
             
            //call server rpc to update table position
            // does a server rpc to update the table position, which checks if the table has one or two players
            // if it has one player, it moves the table to the teleport position
            // if it has two players, it creates a new table and moves the player to the new table
            // TODO: think about how to set rotation?
            playerManager.MovePlayerFromTableToPositionServerRpc(playerId, point, rotation);
        }

        private void OnPointsOfInterestChanged(NetworkListEvent<Vector3> changeEvent)
        {
            if (IsOwner) return;
            if (!IsClient) return;
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
                        playerGestureDetector.RemovePointOfInterest(changeEvent.Value);
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
