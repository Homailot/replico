using System.Collections.Generic;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures
{
    public readonly struct Hands
    {
        public List<Finger> firstHand { get; }
        public List<Finger> secondHand { get; }

        public Hands(List<Finger> firstHand, List<Finger> secondHand)
        {
            this.firstHand = firstHand;
            this.secondHand = secondHand;
        }
        
        public static Hands none => new(new List<Finger>(), new List<Finger>());
        public bool IsEmpty() => firstHand.Count == 0 && secondHand.Count == 0;
    }
}