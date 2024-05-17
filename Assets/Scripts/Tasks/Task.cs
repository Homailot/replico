using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public abstract class Task : MonoBehaviour
    {
        [SerializeField] private TaskFinishedEvent taskFinishedEvent;
        [SerializeField] private float taskTime;
        
        public abstract void StartTask(Logger logger);

        public void EndTask(bool success = false)
        {
            EndTaskInternal(success);
            taskFinishedEvent.Invoke(success); 
        }
        
        public float GetTaskTime()
        {
            return taskTime;
        }
        
        protected abstract void EndTaskInternal(bool success);
        public abstract void CleanTask();
        public abstract bool Next(); 
        
        public void SetOnTaskFinishedListener(UnityAction<bool> action)
        {
            taskFinishedEvent.AddListener(action);
        }
        
        [Serializable]
        public class TaskFinishedEvent : UnityEvent<bool> {}
    }
}