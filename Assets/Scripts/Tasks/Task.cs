using System;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public abstract class Task : MonoBehaviour
    {
        [SerializeField] private TaskFinishedEvent taskFinishedEvent;
        
        public abstract void StartTask(Logger logger);

        public void EndTask(bool success = false)
        {
            EndTask(success);
            taskFinishedEvent.Invoke(success); 
        }
        
        public abstract void CleanTask(bool success);
        public abstract bool Next(); 
        
        public void SetOnTaskFinishedListener(UnityAction<bool> action)
        {
            taskFinishedEvent.AddListener(action);
        }
        
        [Serializable]
        public class TaskFinishedEvent : UnityEvent<bool> {}
    }
}