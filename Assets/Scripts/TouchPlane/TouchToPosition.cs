using Unity.Mathematics;
using UnityEngine;
using Utils;

namespace TouchPlane
{
    public class TouchToPosition : MonoBehaviour
    {
        private Renderer _renderer;
    
        private void Start()
        {
            _renderer = GetComponent<Renderer>();
        }

        public Vector3 GetTouchPosition(Vector2 touchPosition)
        {
            var bounds = _renderer.localBounds;
        
            var min = new float2(bounds.min.x, bounds.min.y);
            var max = new float2(bounds.max.x, bounds.max.y);
            var touch = new float2(Mathf.Clamp(touchPosition.y, 0, Screen.height),
                Mathf.Clamp(touchPosition.x, 0, Screen.width));
        
            var remapped = MathUtils.Remap(touch, new float2(0, 0), new float2(Screen.height, Screen.width), min, max);
            var point = new Vector3(-remapped.x, remapped.y, bounds.center.z);

            return transform.TransformPoint(point);
        }
    }
}