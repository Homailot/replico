using System;
using UnityEngine;

namespace TouchPlane
{
    public class TouchPlaneUV : MonoBehaviour
    {
        private void Start()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
            var screenMax = (float) Mathf.Max(Screen.width, Screen.height);
            var uv = mesh.uv;
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(0, Screen.height / screenMax);
            uv[2] = new Vector2(Screen.width / screenMax, Screen.height / screenMax);
            uv[3] = new Vector2(Screen.width / screenMax, 0);
            mesh.uv = uv;
        }
    }
}