using UnityEngine;

namespace Gestures.Balloon
{
    public class BalloonPoint : MonoBehaviour
    {
        public Vector3 localPosition;

        public void UpdatePosition(Transform parent)
        {
            transform.position = parent.TransformPoint(localPosition);
        }
    }
}