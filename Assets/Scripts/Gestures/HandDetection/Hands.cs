using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures.HandDetection
{
    public readonly struct Hands
    {
        public ISet<Finger> firstHand { get; }
        public ISet<Finger> secondHand { get; }

        public Hands(ISet<Finger> firstHand, ISet<Finger> secondHand)
        {
            this.firstHand = firstHand;
            this.secondHand = secondHand;
        }
        
        public Vector2 GetFirstHandCenter()
        {
            if (firstHand.Count == 0) return Vector2.zero;
            
            var center = Vector2.zero;
            foreach (var finger in firstHand)
            {
                center += finger.screenPosition;
            }
            return center / firstHand.Count;
        }
        
        public Vector2 GetSecondHandCenter()
        {
            if (secondHand.Count == 0) return Vector2.zero;
            
            var center = Vector2.zero;
            foreach (var finger in secondHand)
            {
                center += finger.screenPosition;
            }
            return center / secondHand.Count;
        }
        
        public static Hands none => new(new HashSet<Finger>(), new HashSet<Finger>());
        public bool IsEmpty() => firstHand.Count == 0 && secondHand.Count == 0;
        
        public void Print()
        {
            Debug.Log("First Hand:");
            foreach (var finger in firstHand)
            {
                Debug.Log(finger.screenPosition);
            }
            Debug.Log("Second Hand:");
            foreach (var finger in secondHand)
            {
                Debug.Log(finger.screenPosition);
            }
        }
    }
}