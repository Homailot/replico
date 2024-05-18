using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Tasks
{
    public class Clock : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private float seconds = 0;
        [SerializeField] private TimerEnd timerEnd;
        
        private bool _running;
    
        public void SetTime(float time)
        {
            seconds = time;
            _running = true;
        }
        
        public void ClearTime()
        {
            seconds = 0;
            _running = false;
        }
    
        public void AddTimeEndListener(UnityAction action)
        {
            timerEnd.AddListener(action);
        }
    
        private void Update()
        {
            seconds -= Time.deltaTime;
        
            if (seconds <= 0)
            {
                if (_running)
                {
                    timerEnd.Invoke();
                    _running = false;
                }
                seconds = 0;
            }
        
            var minutes = (int) (seconds / 60);
            const string empty = " ";
            const string colon = ":";
            var colonOrEmpty = (seconds % 1f > 0.5) ? colon : empty;
            //text.text = $"{minutes % 60:D2}{colonOrEmpty}{(int) seconds % 60:D2}";
        }
    
        [Serializable]
        public class TimerEnd : UnityEvent
        {
        }
    }
}