using System;
using System.Collections.Generic;
using Gestures;
using UnityEngine;

namespace Tasks
{
    [RequireComponent(typeof(Outline), typeof(OutlinePulse))]
    public class TaskObjectPoint : MonoBehaviour, IReplicaPoint
    {
        [SerializeField] private float highlightWidth = 4f;
        [SerializeField] private float scaleMultiplier = 1.15f;
        [SerializeField] private Color highlightColor = new Color(0.02745098f, 0.9764706f, 0.5568628f);
        
        private Outline _outline;
        private OutlinePulse _outlinePulse;
        private Vector3 _originalScale;
        private Color _originalHighlightColor;
        private List<Material> _materials = new List<Material>();
        
        private bool _isIntersected;
        private bool _isHighlighted;
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int Glow = Shader.PropertyToID("_Glow");

        public void Awake()
        {
            _outline = GetComponent<Outline>();
            _outlinePulse = GetComponent<OutlinePulse>();
            _originalHighlightColor = _outline.OutlineColor;
            _originalScale = transform.localScale;
            
            _outline.enabled = false;
            _outlinePulse.enabled = false;
             
            selectable = false;
            var renderers = GetComponentsInChildren<Renderer>();
            var mainRenderer = GetComponent<Renderer>();
            
            if (mainRenderer != null)
            {
                _materials.Add(mainRenderer.material);
            }
            foreach (var renderer in renderers)
            {
                _materials.Add(renderer.material);
            }
        }
        
        public void Start()
        {

        }
         
        public void Highlight()
        {
            if (_isHighlighted) return;
            
            _isHighlighted = true;
            _outline.OutlineWidth = highlightWidth;
            _outline.OutlineColor = highlightColor;
            _outlinePulse.enabled = false;
            
            foreach (var material in _materials)
            {
                material.SetColor(Color1, highlightColor);
                material.SetInt(Glow, 0);
            }
            
            transform.localScale *= scaleMultiplier;
        }

        public void Unhighlight()
        {
            if (!_isHighlighted) return;
            
            _isHighlighted = false;
            _outline.OutlineColor = _originalHighlightColor;
            _outlinePulse.enabled = true;
            
            foreach (var material in _materials)
            {
                material.SetColor(Color1, Color.white);
                material.SetInt(Glow, 1);
            }
            
            transform.localScale = _originalScale;
        }
        
        public bool Intersects()
        {
            return _isIntersected;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        public bool selectable { get; set; }
        public void OnSelect(GestureDetector gestureDetector)
        {
            throw new System.NotImplementedException();
        }

        private void OnTriggerEnter(Collider other)
        {
            _isIntersected = true;
        }
        
        private void OnTriggerExit(Collider other)
        {
            _isIntersected = false;
        }
        
        public void PrepareTaskObject()
        {
            selectable = true;
            _outline.enabled = true;
            _outlinePulse.enabled = true;

            foreach (var material in _materials)
            {
                material.SetColor(Color1, Color.white);
                material.SetInt(Glow, 1);
            }
        }
        
        public void ResetTaskObject()
        {
            selectable = false;
            _outline.enabled = false;
            _outlinePulse.enabled = false;
            
            foreach (var material in _materials)
            {
                material.SetColor(Color1, Color.white);
                material.SetInt(Glow, 0);
            }
        }
    }
}