using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Player;
using Replica;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tasks
{
    public class FourthTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private ReplicaController replicaController;
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private InputActionReference successAction;
        [SerializeField] private InputActionReference failureAction;
        
        [SerializeField] private CollaborativeTaskNetwork collaborativeTaskNetwork;
        
        private TaskGroupObjects _taskObjectsScript;
        private Logger _logger;
        private Tasks _tasks;

        public override void StartTask(Tasks tasks, Logger logger)
        {
            _logger = logger;
            _tasks = tasks;
            
            gestureDetector.ClearPointsOfInterest();
            
            var playerId = gestureDetector.GetPlayerId();

            if (playerId == 1)
            {
                NetworkManager.Singleton.Shutdown();

                StartCoroutine(WaitForConnection());
            }
            else
            { 
                var endTransform = replicaController.GetEndTransform();
                replicaController.SetTarget(endTransform);
                _taskObjectsScript = replicaController.GetReplica().GetComponent<TaskGroupObjects>();
                
                playerManager.MovePlayerFromTableToStartPositionServerRpc(gestureDetector.GetPlayerId());

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

                successAction.action.performed += OnSuccess;
                failureAction.action.performed += OnFailure;

                collaborativeTaskNetwork.StartNextTask();
            }
        }
        
        private void OnSuccess(InputAction.CallbackContext context)
        {
            collaborativeTaskNetwork.EndFourthTaskRpc(true);
            successAction.action.performed -= OnSuccess;
            failureAction.action.performed -= OnFailure;
        }
        
        private void OnFailure(InputAction.CallbackContext context)
        {
            collaborativeTaskNetwork.EndFourthTaskRpc(false);
            failureAction.action.performed -= OnFailure;
            successAction.action.performed -= OnSuccess;
        }
        
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log("Client connected on task 4");
            _logger.StartTask();
            _taskObjectsScript.taskGroupPoints[0].PrepareTaskObject();
            
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
        
        private void OnServerConnected(NetworkManager networkManager, ConnectionEventData e)
        {
            if (e.EventType == ConnectionEvent.ClientConnected)
            {
                Debug.Log("Connected to server");
                _logger.StartTask();
                
                NetworkManager.Singleton.OnConnectionEvent -= OnServerConnected;
            }
        }
        
        private IEnumerator WaitForConnection()
        {
            yield return new WaitForSeconds(2);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                _tasks.serverIp, _tasks.serverPort
                );

            NetworkManager.Singleton.OnConnectionEvent += OnServerConnected;
            NetworkManager.Singleton.StartClient();
        }

        protected override void EndTaskInternal(bool success)
        {
            CleanTask();
            _logger.EndTask(success); 
        }

        public override void CleanTask()
        {
            if (_taskObjectsScript != null)
            {
                _taskObjectsScript.taskGroupPoints[0].ResetTaskObject();
            } 
            
            gestureDetector.ClearPointsOfInterest();
        }
    }
}