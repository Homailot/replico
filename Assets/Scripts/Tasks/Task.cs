using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public abstract class Task : MonoBehaviour
    {
        [SerializeField] private TaskFinishedEvent taskFinishedEvent;
        [SerializeField] private float taskTime;

        private bool _failed;
        private bool _started;
        
        public void BeginTask(Tasks tasks, Logger logger)
        {
            _failed = false;
            _started = true;
            StartTask(tasks, logger);
        }

        protected abstract void StartTask(Tasks tasks, Logger logger);

        public void EndTask(bool success = false)
        {
            if (!_started)
            {
                return;
            }
            _started = false;
            
            if (_failed)
            {
                success = false;
            }
            EndTaskInternal(success);
            taskFinishedEvent.Invoke(success); 
        }
        
        public float GetTaskTime()
        {
            return taskTime;
        }
        
        protected abstract void EndTaskInternal(bool success);
        public abstract void CleanTask();

        public void SetFailed()
        {
            _failed = true;
        }
        
        public void SetOnTaskFinishedListener(UnityAction<bool> action)
        {
            taskFinishedEvent.AddListener(action);
        }
        
        [Serializable]
        public class TaskFinishedEvent : UnityEvent<bool> {}
    }
}