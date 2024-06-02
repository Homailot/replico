using System;
using UnityEngine;

namespace TouchPlane
{
    public class TouchPlaneUV : MonoBehaviour
    {
        private int _lastScreenWidth;
        private int _lastScreenHeight;
        
        private void Start()
        {
            UpdateUV();
        }

        private void UpdateUV()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            var screenMax = (float) Mathf.Max(Screen.width, Screen.height);
            var uv = mesh.uv;
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(0, Screen.height / screenMax);
            uv[2] = new Vector2(Screen.width / screenMax, Screen.height / screenMax);
            uv[3] = new Vector2(Screen.width / screenMax, 0);
            mesh.uv = uv;
            
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
        }

        private void Update()
        {
            if (_lastScreenHeight == Screen.height && _lastScreenWidth == Screen.width) return;
            
            UpdateUV();
        } 
    }
}