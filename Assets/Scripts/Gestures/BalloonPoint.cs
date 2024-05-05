using UnityEngine;

namespace Gestures
{
    public class BalloonPoint : MonoBehaviour, IReplicaPoint
    {
        [SerializeField] private GameObject highlightPrefab;
        private bool _isHighlighted;
        private GameObject _instantiatedHighlight; 
        
        public ulong playerId;
        public Vector3 localPosition;

        public void UpdatePosition(Transform parent)
        {
            transform.position = parent.TransformPoint(localPosition);
            transform.rotation = parent.rotation;
        }

        public void Highlight()
        {
            if (_instantiatedHighlight == null)
            {
                _instantiatedHighlight = Instantiate(highlightPrefab, transform);
            }
            _isHighlighted = true;
        }

        public void Unhighlight()
        {
            if (_instantiatedHighlight != null)
            {
                Destroy(_instantiatedHighlight);
                _instantiatedHighlight = null;
            }
            _isHighlighted = false;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        public void OnSelect(GestureDetector gestureDetector)
        {
            gestureDetector.RemovePointOfInterest(this);
        }
    }
}