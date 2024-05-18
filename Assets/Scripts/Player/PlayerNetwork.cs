using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Gestures.Balloon;
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
        private struct PointOfInterestData : INetworkSerializable, IEquatable<PointOfInterestData>
        {
            private float _x, _y, _z;
            private ulong _playerId;
            private ulong _id;
            
            internal Vector3 position
            {
                get => new Vector3(_x, _y, _z);
                set => (_x, _y, _z) = (value.x, value.y, value.z);
            }
            
            internal ulong playerId
            {
                get => _playerId;
                set => _playerId = value;
            }
            
            internal ulong id
            {
                get => _id;
                set => _id = value;
            }
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref _x);
                serializer.SerializeValue(ref _y);
                serializer.SerializeValue(ref _z);
                serializer.SerializeValue(ref _playerId);
                serializer.SerializeValue(ref _id);
            }

            public bool Equals(PointOfInterestData other)
            {
                return _x.Equals(other._x) && _y.Equals(other._y) && _z.Equals(other._z) && _playerId == other._playerId && _id == other._id;
            }

            public override bool Equals(object obj)
            {
                return obj is PointOfInterestData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(_x, _y, _z, _playerId, _id);
            }
        }
        
        [Header("Local Player Components")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Camera uiCamera;
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

        private NetworkList<PointOfInterestData> _pointsOfInterest;
        
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

        private PlayerManager playerManager { get; set; }

        private void Awake()
        {
            _xrOrigin = GetComponent<XROrigin>();
            
            playerTransform.xrOrigin = _xrOrigin;
            _pointsOfInterest = new NetworkList<PointOfInterestData>(writePerm: NetworkVariableWritePermission.Server);
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
                uiCamera.enabled = true;
                audioListener.enabled = true;
                trackedPoseDriver.enabled = true;
                rightController.SetActive(true);
                leftController.SetActive(true);
                xrOrigin.enabled = true;
                characterController.enabled = true;
                characterControllerDriver.enabled = true;
                xrInputModalityManager.enabled = true;
                
                NetworkManager.SceneManager.OnSceneEvent += OnSceneEvent;
            }
            else
            {
                playerModel.SetActive(true);
                _playerId.OnValueChanged += OnPlayerIdChanged;
                OnPlayerIdChanged(0, playerId);
            }
        }

        private void OnSceneEvent(SceneEvent sceneevent)
        {
            if (sceneevent.SceneEventType == SceneEventType.Load)
            {
                _initialized = false;
                _touchPlane = null;
                _pointsOfInterest.Clear();
            }
            else if (sceneevent.SceneEventType == SceneEventType.LoadEventCompleted)
            {
                playerManager = FindObjectOfType<PlayerManager>();
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
            
            touchPlane = GameObject.FindWithTag("Frame");
            touchPlane.transform.position = position;
            touchPlane.transform.rotation = attachPoint.rotation;
            
            gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
            gestureDetector.Init();
            gestureDetector.SetPlayerId(playerId);

            foreach (var playerObject in FindObjectsByType<PlayerNetwork>(FindObjectsSortMode.None))
            {
                foreach (var point in playerObject._pointsOfInterest)
                {
                    gestureDetector.AddPointOfInterest(point.position, playerObject.playerId, point.id);
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
            gestureDetector.AddTableSelectedListener(OnTableSelected);
            gestureDetector.AddPointCountResetListener(OnPointCountReset);
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
            CreatePointOfInterestRpc(point); 
        }

        [Rpc(SendTo.Server)]
        private void CreatePointOfInterestRpc(Vector3 point)
        {
            var id = playerManager.IncrementAndGetBalloonId();
            _pointsOfInterest.Add(new PointOfInterestData
            { 
                position = point, 
                playerId = playerId, 
                id = id
            });
        }
        
        private void OnPointCountReset()
        {
            if (!IsOwner) return;
            ResetPointOfInterestCounterRpc();
        }
        
        [Rpc(SendTo.Server)]
        private void ResetPointOfInterestCounterRpc()
        {
            playerManager.ResetBalloonId();
        }
        
        private void OnPointRemoved(BalloonPointId point)
        {
            if (!IsOwner) return;
            RemovePointOfInterestRpc(point.id, playerId);
        }
        
        [Rpc(SendTo.Server)]
        private void RemovePointOfInterestRpc(ulong id, ulong playerId)
        {
            foreach (var pointOfInterest in _pointsOfInterest)
            {
                if (pointOfInterest.id == id && pointOfInterest.playerId == playerId)
                {
                    _pointsOfInterest.Remove(pointOfInterest);
                    return;
                }
            }
        }

        private void OnTeleportSelected(Vector3 point, Quaternion rotation)
        {
            if (!IsOwner) return;
             
            playerManager.MovePlayerFromTableToPositionServerRpc(playerId, point, rotation);
        }

        private void OnTableSelected(ulong tableId)
        {
            if (!IsOwner) return;
            
            playerManager.MovePlayerToTableServerRpc(playerId, tableId);
        }

        private void OnPointsOfInterestChanged(NetworkListEvent<PointOfInterestData> changeEvent)
        {
            if (IsOwner)
            {
                switch (changeEvent.Type)
                {
                    case NetworkListEvent<PointOfInterestData>.EventType.Add:
                    case NetworkListEvent<PointOfInterestData>.EventType.Insert:
                        gestureDetector.UpdateBalloonId(_playerId.Value, changeEvent.Value.position, changeEvent.Value.id);
                        break;
                    case NetworkListEvent<PointOfInterestData>.EventType.Remove:
                        gestureDetector.RemovePointOfInterest(changeEvent.Value.id, playerId);
                        break;
                }

                return;
            }
            if (!IsClient) return;
            var player = NetworkManager.LocalClient.PlayerObject.GetComponent<PlayerNetwork>();
            var playerGestureDetector = player.gestureDetector;
            
            switch (changeEvent.Type)
            {
                case NetworkListEvent<PointOfInterestData>.EventType.Add:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.AddPointOfInterest(changeEvent.Value.position, playerId, changeEvent.Value.id);
                    }
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.Insert:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.AddPointOfInterest(changeEvent.Value.position, playerId, changeEvent.Value.id);
                    }
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.Remove:
                    if (playerGestureDetector != null)
                    {
                        playerGestureDetector.RemovePointOfInterest(changeEvent.Value.id, playerId);
                    }
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.RemoveAt:
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.Value:
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.Clear:
                    break;
                case NetworkListEvent<PointOfInterestData>.EventType.Full:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
