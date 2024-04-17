using System.Collections.Generic;
using UnityEngine.InputSystem.EnhancedTouch;

namespace Gestures
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
        
        public static Hands none => new(new HashSet<Finger>(), new HashSet<Finger>());
        public bool IsEmpty() => firstHand.Count == 0 && secondHand.Count == 0;
    }
}