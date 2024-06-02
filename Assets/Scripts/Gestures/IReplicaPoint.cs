using System.Numerics;

namespace Gestures
{
    public interface IReplicaPoint
    {
        public void Highlight();
        public void Unhighlight();
        public bool Intersects();
        public bool IsHighlighted();
        public bool selectable { get; set; }
        public void OnSelect(GestureDetector gestureDetector);
    }
}