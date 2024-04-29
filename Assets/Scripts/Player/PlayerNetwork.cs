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
        private bool _inTable;

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
                StartCoroutine(FirstAttach(table, seat));
            }
        }

        private void ChangeSeat(Table table, int seat)
        { 
            var attachPoint = seat == 0 ? table.firstAttach : table.secondAttach; 
            var trackerToOrigin = playerCamera.transform.position - tracker.position;
            var position = attachPoint.position;
            playerTransform.SetTransform(position + trackerToOrigin, attachPoint.up, attachPoint.forward);
            var touchPlane = Instantiate(touchPlanePrefab, position, attachPoint.rotation);
            
            var replicaController = touchPlane.GetComponentInChildren<ReplicaController>();
            replicaController.SetObjectToReplicate(GameObject.FindWithTag("ToReplicate"));
            
            gestureDetector = touchPlane.GetComponentInChildren<GestureDetector>();
            gestureDetector.Init();
            // TODO: all previous points of interest
            gestureDetector.AddPointSelectedListener(OnPointSelected);
        }
        
        private IEnumerator FirstAttach(Table table, int seat)
        {
            yield return new WaitForSeconds(1);
            ChangeSeat(table, seat);
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
