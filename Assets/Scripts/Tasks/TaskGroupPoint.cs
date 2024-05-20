using System;
using System.Collections.Generic;
using Gestures;
using UnityEngine;

namespace Tasks
{
    [RequireComponent(typeof(Outline), typeof(OutlinePulse))]
    public class TaskGroupPoint : MonoBehaviour, IReplicaPoint
    {
        private Outline _outline;
        private OutlinePulse _outlinePulse;
        private readonly List<Material> _materials = new List<Material>();
        
        private bool _isIntersected;
        private bool _isHighlighted;
        private static readonly int Color1 = Shader.PropertyToID("_Color");
        private static readonly int Glow = Shader.PropertyToID("_Glow");

        public void Awake()
        {
            _outline = GetComponent<Outline>();
            _outlinePulse = GetComponent<OutlinePulse>();
            
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
        
        public void Highlight()
        {
            if (_isHighlighted) return;
            
            _isHighlighted = true;
        }

        public void Unhighlight()
        {
            if (!_isHighlighted) return;
            _isHighlighted = false;
        }
        
        public bool Intersects()
        {
            return false;
        }

        public bool IsHighlighted()
        {
            return _isHighlighted;
        }

        public bool selectable { get; set; }
        public void OnSelect(GestureDetector gestureDetector)
        {
        }

        public void PrepareTaskObject()
        {
            selectable = false;
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