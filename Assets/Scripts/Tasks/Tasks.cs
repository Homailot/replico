using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public class Tasks : MonoBehaviour
    {
        [SerializeField] private List<Task> tasks;
        [SerializeField] private Clock clock;
        
        private Logger _logger;
        private int _currentTaskIndex = -1;
        private bool _inTask;

        public string serverIp { get; set; }
        public ushort serverPort { get; set; }
        public ushort clientPort { get; set; }

        public void Start()
        {
            clock.AddTimeEndListener(() =>
            {
                if (_inTask)
                {
                    tasks[_currentTaskIndex].SetFailed();
                }
            });
        }

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
                tasks[_currentTaskIndex].SetOnTaskFinishedListener((_) =>
                {
                    _inTask = false;
                    clock.ClearTime();
                    tasks[_currentTaskIndex].CleanTask();
                });
                tasks[_currentTaskIndex].StartTask(this, _logger);
                clock.SetTime(tasks[_currentTaskIndex].GetTaskTime());
                _inTask = true;
            }
        }
        
        public void SkipTask(bool success = false)
        {
            if (_inTask)
            {
                tasks[_currentTaskIndex].EndTask(false);
                _inTask = false;
                clock.ClearTime();
            }
        }
    }
}