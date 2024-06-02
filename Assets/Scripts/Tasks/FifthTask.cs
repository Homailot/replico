using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Player;
using Replica;
using Tables;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tasks
{
    public class FifthTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private ReplicaController replicaController;
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private InputActionReference successAction;
        [SerializeField] private InputActionReference failureAction;
        
        [SerializeField] private CollaborativeTaskNetwork collaborativeTaskNetwork;
        
        private TaskGroupObjects _taskObjectsScript;
        private Logger _logger;

        protected override void StartTask(Tasks _, Logger logger)
        {
            _logger = logger; 
            gestureDetector.ClearPointsOfInterest();
            var endTransform = replicaController.GetEndTransform();
            replicaController.SetTarget(endTransform);
            _taskObjectsScript = replicaController.GetReplica().GetComponent<TaskGroupObjects>();
           
            _logger.StartTask();
            var playerId = gestureDetector.GetPlayerId();

            if (playerId == 0)
            { 
                successAction.action.performed += OnSuccess;
                failureAction.action.performed += OnFailure;
                playerManager.MoveBothPlayersToNewTableServerRpc();
                collaborativeTaskNetwork.StartNextTask();
            }
            else
            {
                _taskObjectsScript.taskGroupPoints[1].PrepareTaskObject();
            }
        }

        private void OnSuccess(InputAction.CallbackContext context)
        {
            EndTask(true);
            successAction.action.performed -= OnSuccess;
            failureAction.action.performed -= OnFailure;
        }
        
        private void OnFailure(InputAction.CallbackContext context)
        {
            EndTask();
            failureAction.action.performed -= OnFailure;
            successAction.action.performed -= OnSuccess;
        }
        
        protected override void EndTaskInternal(bool success)
        {
            CleanTask();
            collaborativeTaskNetwork.EndFifthTaskRpc(success);
            _logger.EndTask(success); 
        }

        public override void CleanTask()
        {
            if (_taskObjectsScript != null)
            {
                _taskObjectsScript.taskGroupPoints[1].ResetTaskObject();
            } 
             
            gestureDetector.ClearPointsOfInterest();
            
            successAction.action.performed -= OnSuccess;
            failureAction.action.performed -= OnFailure;
        }
    }
}