using System;
using System.Collections.Generic;
using Gestures;
using UnityEngine;

namespace Tasks
{
    [RequireComponent(typeof(Outline), typeof(OutlinePulse))]
    public class TaskTeleportPoint : MonoBehaviour, IReplicaPoint
    {
        [SerializeField] private float highlightWidth = 4f;
        [SerializeField] private float scaleMultiplier = 1.15f;
        [SerializeField] private Color highlightColor = new Color(0.02745098f, 0.9764706f, 0.5568628f);
        [SerializeField] private TeleportZoneHighlight teleportZoneHighlight;
        [SerializeField] private float angleThreshold = 30f;
        
        private Outline _outline;
        private OutlinePulse _outlinePulse;
        private Vector3 _originalScale;
        private Color _originalHighlightColor;
        
        private bool _isHighlighted;
        private bool _intersects;
        private GestureDetector _gestureDetector;

        public void Awake()
        {
            _outline = GetComponent<Outline>();
            _outlinePulse = GetComponent<OutlinePulse>();
            _originalHighlightColor = _outline.OutlineColor;
            _originalScale = transform.localScale;
            
            _outline.enabled = false;
            _outlinePulse.enabled = false;
            selectable = false;
        }

        public void Start()
        {
            teleportZoneHighlight.gameObject.SetActive(false);
        }

        public void Highlight()
        {
            if (_isHighlighted) return;
            if (!Intersects()) return;
            
            _isHighlighted = true;
            _outline.OutlineWidth = highlightWidth;
            _outline.OutlineColor = highlightColor;
            _outlinePulse.enabled = false;
            
            transform.localScale *= scaleMultiplier;
        }

        public void Unhighlight()
        {
            if (!_isHighlighted) return;
            
            _isHighlighted = false;
            _outline.OutlineColor = _originalHighlightColor;
            _outlinePulse.enabled = true;
            
            transform.localScale = _originalScale;
        }
        
        public bool InsideZone()
        {
            return teleportZoneHighlight.Intersects();
        }
        
        public bool Intersects()
        {
            _intersects = false;
            if (!teleportZoneHighlight.Intersects())
            {
                return false;
            }

            if (_gestureDetector == null)
            {
                return false;
            }

            if (!_gestureDetector.ArrowEnabled())
            {
                return false;
            }

            // check if balloon rotation is toward the object in the y axis
            var balloonRotationQuat = _gestureDetector.GetBalloonRotation();
            var balloonRotation = balloonRotationQuat.eulerAngles;
            var objectPosition = transform.localPosition;
            var objectPosition2D = new Vector2(objectPosition.x, objectPosition.z);
            
            var balloonPosition = _gestureDetector.GetBalloonPosition();
            var balloonPosition2D = new Vector2(balloonPosition.x, balloonPosition.z);
            
            var balloonToObject = objectPosition2D - balloonPosition2D;
            var balloonToRotation = Quaternion.Euler(0, balloonRotation.y, 0) * Vector3.forward;
            var angle = Vector2.Angle(balloonToObject, new Vector2(balloonToRotation.x, balloonToRotation.z));

            _intersects = angle < angleThreshold;
            return _intersects;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        public bool selectable { get; set; }
        public void OnSelect(GestureDetector gestureDetector)
        {
            gestureDetector.OnPointSelected();
            //gestureDetector.OnTaskObjectSelected();
        }

        public void OnTeleportSelected(Vector3 position, Quaternion rotation)
        {
            Debug.Log("Teleporting to " + position);
            if (!selectable) return;
            Debug.Log("selectable");
            if (!teleportZoneHighlight.Intersects())
            {
                return;
            }
             // check if balloon rotation is toward the object in the y axis
            var balloonRotation = rotation.eulerAngles;
            var objectPosition = transform.localPosition;
            var objectPosition2D = new Vector2(objectPosition.x, objectPosition.z);
            
            var balloonPosition2D = new Vector2(position.x, position.z);
            
            var balloonToObject = objectPosition2D - balloonPosition2D;
            var balloonToRotation = Quaternion.Euler(0, balloonRotation.y, 0) * Vector3.forward;
            var angle = Vector2.Angle(balloonToObject, new Vector2(balloonToRotation.x, balloonToRotation.z));

            _intersects = angle < angleThreshold;
            if (!_intersects) return;
            Debug.Log("intersects");
            
            _gestureDetector.OnTaskObjectSelected();
        }

        
        public void PrepareTaskTeleport(GestureDetector gestureDetector)
        {
            selectable = true;
            _outline.enabled = true;
            _outlinePulse.enabled = true;
            teleportZoneHighlight.gameObject.SetActive(true);
            _gestureDetector = gestureDetector;
            _gestureDetector.AddTeleportSelectedListener(OnTeleportSelected);
        }
        
        public void ResetTaskTeleport(GestureDetector gestureDetector)
        {
            selectable = false;
            _outline.enabled = false;
            _outlinePulse.enabled = false;
            teleportZoneHighlight.gameObject.SetActive(false);
            gestureDetector.RemoveTeleportSelectedListener(OnTeleportSelected);
            _gestureDetector = null;
        }
    }
}