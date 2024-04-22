using Unity.Mathematics;
using UnityEngine;

namespace Utils
{
    public class MathUtils
    {
        public static float2 Remap(float2 value, float2 low1, float2 high1, float2 low2, float2 high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1); 
        }
        
        public static Vector2 ScreenSpaceToRelative(Vector2 screenSpace, float screenHeight, float screenWidth)
        {
            var max = Mathf.Max(screenWidth, screenHeight);
            return new Vector2(screenSpace.x, screenSpace.y) / max;
        }
    }
}