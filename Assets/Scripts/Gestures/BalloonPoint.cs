using UnityEngine;

namespace Gestures
{
    public class BalloonPoint : MonoBehaviour, IReplicaPoint
    {
        [SerializeField] private GameObject highlightPrefab;
        private bool _isHighlighted;
        private GameObject _instantiatedHighlight; 
        private bool _isIntersected;
        
        public ulong playerId;
        public Vector3 localPosition;
        
        public bool selectable { get; set; }

        public void UpdatePosition(Transform parent)
        {
            transform.position = parent.TransformPoint(localPosition);
            transform.rotation = parent.rotation;
        }
        
        public void SetHighlight(GameObject highlight)
        {
            highlightPrefab = highlight;
        }

        public void Highlight()
        {
            if (!selectable) return;
            if (!_isHighlighted)
            {
                _instantiatedHighlight = Instantiate(highlightPrefab, transform);
            }
            _isHighlighted = true;
        }

        public void Unhighlight()
        {
            if (!selectable) return;
            if (_isHighlighted)
            {
                Destroy(_instantiatedHighlight);
                _instantiatedHighlight = null;
            }
            _isHighlighted = false;
        }

        public bool Intersects()
        {
            return _isIntersected;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        public void OnSelect(GestureDetector gestureDetector)
        {
            if (!selectable) return;
            gestureDetector.OnPointRemoved(this);
        }
        
        private void OnTriggerEnter(Collider other)
        {
            _isIntersected = true;
        }
        
        private void OnTriggerExit(Collider other)
        {
            _isIntersected = false;
        }
    }
}