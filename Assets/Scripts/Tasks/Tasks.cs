using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public class Tasks : MonoBehaviour
    {
        [SerializeField] private List<Task> tasks;
        
        private Logger _logger;
        private int _currentTaskIndex = 0;
        private bool _inTask;
        
        public void SetLogger(Logger logger)
        {
            _logger = logger;
        }
        
        public void StartOrSkipTask(bool success = false)
        {
            if (_inTask)
            {
                SkipTask(success);
                return;
            }
            
            _currentTaskIndex++;
            if (_currentTaskIndex < tasks.Count)
            {
                tasks[_currentTaskIndex].SetOnTaskFinishedListener((_) => _inTask = false);
                tasks[_currentTaskIndex].StartTask(_logger);
                _inTask = true;
            }
        }
        
        public void SkipTask(bool success = false)
        {
            if (_inTask)
            {
                tasks[_currentTaskIndex].EndTask(false);
                _inTask = false;
            }
        }
        

    }
}