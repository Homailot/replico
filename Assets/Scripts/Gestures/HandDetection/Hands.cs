using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures.HandDetection
{
    public readonly struct Hands
    {
        public HashSet<Finger> firstHand { get; }
        public HashSet<Finger> secondHand { get; }

        public Hands(HashSet<Finger> firstHand, HashSet<Finger> secondHand)
        {
            this.firstHand = firstHand;
            this.secondHand = secondHand;
        }
        
        public Vector2 GetFirstHandCenter()
        {
            var center = Vector2.zero;
            foreach (var finger in firstHand)
            {
                center += finger.screenPosition;
            }
            return center / firstHand.Count;
        }
        
        public Vector2 GetSecondHandCenter()
        {
            var center = Vector2.zero;
            foreach (var finger in secondHand)
            {
                center += finger.screenPosition;
            }
            return center / secondHand.Count;
        }
        
        public static Hands none => new(new HashSet<Finger>(), new HashSet<Finger>());
        public bool IsEmpty() => firstHand.Count == 0 && secondHand.Count == 0;
    }
}