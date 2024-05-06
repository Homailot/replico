using System;
using UnityEngine;

namespace Gestures
{
    public class TablePoint : MonoBehaviour, IReplicaPoint
    {
        [SerializeField] private Transform firstAttach;
        [SerializeField] private Transform secondAttach;
        [SerializeField] private float highlightWidth;
        [SerializeField] private float scaleMultiplier;  

        public Vector3 localPosition { get; set; }
        public Quaternion localRotation { get; set; }
        public ulong firstPlayerId { get; set; }
        public ulong secondPlayerId { get; set; }
        public bool selectable { get; set; }
        
        private GameObject _firstPlayer;
        private GameObject _secondPlayer;
        private Outline _outline;
        private float _originalOutlineWidth;
        private Vector3 _originalScale;
        
        private bool _isIntersected;
        private bool _isHighlighted;
        
        public void Awake()
        {
            firstPlayerId = ulong.MaxValue;
            secondPlayerId = ulong.MaxValue;
            selectable = true;
        }

        public void Start()
        {
            _outline = GetComponent<Outline>();
            _originalOutlineWidth = _outline.OutlineWidth;
            _originalScale = transform.localScale;
        }

        public void UpdatePosition(Transform parent)
        {
            transform.position = parent.TransformPoint(localPosition);
            transform.rotation = parent.rotation * localRotation;
        }

        public void AttachPlayer(GameObject playerPrefab, ulong playerId, int seat)
        {
            switch (seat)
            {
                case 0:
                    if (_firstPlayer != null)
                    {
                        Destroy(_firstPlayer);
                    }

                    _firstPlayer = Instantiate(playerPrefab, firstAttach);
                    _firstPlayer.transform.localRotation = Quaternion.identity;
                    
                    firstPlayerId = playerId;
                    break;
                case 1:
                    if (_secondPlayer != null)
                    {
                        Destroy(_secondPlayer);
                    }
                    _secondPlayer = Instantiate(playerPrefab, secondAttach);
                    _secondPlayer.transform.localRotation = Quaternion.identity;
                    
                    secondPlayerId = playerId;
                    break;
            }
        } 
        
        public void DetachPlayer(ulong playerId)
        {
            if (firstPlayerId == playerId)
            {
                Destroy(_firstPlayer);
                _firstPlayer = null;
                firstPlayerId = ulong.MaxValue;
            }
            else if (secondPlayerId == playerId)
            {
                Destroy(_secondPlayer);
                _secondPlayer = null;
                secondPlayerId = ulong.MaxValue;
            }
        }

        public void Highlight()
        {
            if (!selectable) return;
            if (!_isHighlighted)
            {
                _outline.OutlineWidth = highlightWidth;
                transform.localScale = _originalScale * scaleMultiplier;
            }
            _isHighlighted = true;
        }

        public void Unhighlight()
        {
            if (!selectable) return;
            if (_isHighlighted)
            {
                _outline.OutlineWidth = _originalOutlineWidth;
                transform.localScale = _originalScale;
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