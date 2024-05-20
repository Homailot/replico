using System;
using System.Collections;
using System.Collections.Generic;
using Gestures;
using Replica;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Tasks
{
    public class FourthTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private ReplicaController replicaController;
        [SerializeField] private string ip;
        [SerializeField] private ushort port;
        
        private TaskObjects _taskObjectsScript;
        private int _currentTaskObjectIndex;
        public Logger _logger;

        public override void StartTask(Logger logger)
        {
            _currentTaskObjectIndex = 0;

           // _taskObjectsScript = replicaController.GetReplica().GetComponent<TaskObjects>();
           // _taskObjectsScript.taskObjectPoints[_currentTaskObjectIndex].PrepareTaskObject();
           _logger = logger;
            
            gestureDetector.ClearPointsOfInterest();
            
            var playerId = gestureDetector.GetPlayerId();

            if (playerId == 1)
            {
                NetworkManager.Singleton.Shutdown();

                StartCoroutine(WaitForConnection());
            }
        }
        
        private IEnumerator WaitForConnection()
        {
            yield return new WaitForSeconds(2);
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                ip, port
                );
            
            NetworkManager.Singleton.StartClient();
        }

        protected override void EndTaskInternal(bool success)
        {
            CleanTask();
            _logger.EndTask(success); 
        }

        public override void CleanTask()
        {
//            foreach (var taskObject in _taskObjectsScript.taskObjectPoints)
//            {
//                taskObject.ResetTaskObject();
 //           }
            
            gestureDetector.ClearPointsOfInterest();
        }

        private void PointSelected()
        {
            var finished = Next();
            if (finished)
            {
                EndTask(true);
            }
        }
        
        public override bool Next()
        {
            _taskObjectsScript.taskObjectPoints[_currentTaskObjectIndex].ResetTaskObject();
            _currentTaskObjectIndex++;
            _logger.TaskStep();
            if (_currentTaskObjectIndex < _taskObjectsScript.taskObjectPoints.Length)
            {
                _taskObjectsScript.taskObjectPoints[_currentTaskObjectIndex].PrepareTaskObject();
            }
            else
            {
                return true;
            }
            
            return false;
        } 
    }
}