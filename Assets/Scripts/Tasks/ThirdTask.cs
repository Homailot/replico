using Gestures;
using Player;
using Replica;
using UnityEngine;

namespace Tasks
{
    public class ThirdTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private ReplicaController replicaController;
        [SerializeField] private PlayerManager playerManager;
        
        private TaskTeleports _taskObjectsScript;
        private int _currentTaskTeleportIndex;
        private Logger _logger;

        public override void StartTask(Logger logger)
        {
            _currentTaskTeleportIndex = 0;

            _taskObjectsScript = replicaController.GetReplica().GetComponent<TaskTeleports>();
            _taskObjectsScript.taskTeleportPoints[_currentTaskTeleportIndex].PrepareTaskTeleport(gestureDetector);
            
            gestureDetector.ClearPointsOfInterest();
            gestureDetector.AddTaskTeleportPoints(_taskObjectsScript.taskTeleportPoints);
            gestureDetector.AddTaskObjectSelectedListener(PointSelected);
            var endTransform = replicaController.GetEndTransform();
            replicaController.SetTarget(endTransform);
            
            playerManager.MovePlayerFromTableToStartPositionServerRpc(gestureDetector.GetPlayerId());
            
            
            _logger = logger;
            _logger.StartTask();
        }

        protected override void EndTaskInternal(bool success)
        {
            CleanTask();
            _logger.EndTask(success); 
        }

        public override void CleanTask()
        {
            foreach (var taskObject in _taskObjectsScript.taskTeleportPoints)
            {
                taskObject.ResetTaskTeleport(gestureDetector);
            }
            
            gestureDetector.ClearTaskTeleportPoints();
            gestureDetector.ClearPointsOfInterest();
            gestureDetector.ClearTaskObjectSelectedListeners();
        }

        private void PointSelected()
        {
            var finished = Next();
            if (finished)
            {
                EndTask(true);
            }
        }

        private bool Next()
        {
            _taskObjectsScript.taskTeleportPoints[_currentTaskTeleportIndex].ResetTaskTeleport(gestureDetector);
            _currentTaskTeleportIndex++;
            _logger.TaskStep();
            if (_currentTaskTeleportIndex < _taskObjectsScript.taskTeleportPoints.Length)
            {
                _taskObjectsScript.taskTeleportPoints[_currentTaskTeleportIndex].PrepareTaskTeleport(gestureDetector);
            }
            else
            {
                return true;
            }
            
            return false;
        } 
    }
}