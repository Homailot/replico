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
            var bounds = _renderer.bounds;
        
            var min = new float2(bounds.min.x, bounds.min.z);
            var max = new float2(bounds.max.x, bounds.max.z);
            var touch = new float2(Mathf.Clamp(touchPosition.x, 0, Screen.width), Mathf.Clamp(touchPosition.y, 0, Screen.height));
        
            var remapped = MathUtils.Remap(touch, new float2(0, 0), new float2(Screen.width, Screen.height), min, max);
            return new Vector3(remapped.x, bounds.center.y, remapped.y);
        }
    }
}