using Gestures;
using Replica;
using UnityEngine;

namespace Tasks
{
    public class FirstTask : Task
    {
        [SerializeField] private GestureDetector gestureDetector;
        [SerializeField] private ReplicaController replicaController;
        
        private TaskObjects _taskObjectsScript;
        private int _currentTaskObjectIndex;
        private Logger _logger;

        public override void StartTask(Logger logger)
        {
            _currentTaskObjectIndex = 0;

            _taskObjectsScript = replicaController.GetReplica().GetComponent<TaskObjects>();
            _taskObjectsScript.taskObjectPoints[_currentTaskObjectIndex].PrepareTaskObject();
            
            gestureDetector.AddTaskPoints(_taskObjectsScript.taskObjectPoints);
            gestureDetector.AddTaskObjectSelectedListener(PointSelected);
            
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
            foreach (var taskObject in _taskObjectsScript.taskObjectPoints)
            {
                taskObject.ResetTaskObject();
            }
            
            gestureDetector.ClearTaskPoints();
            gestureDetector.ClearPointsOfInterest();
            gestureDetector.ClearTaskObjectSelectedListeners();
        }

        private void PointSelected(TaskObjectPoint _)
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