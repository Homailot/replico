using Unity.Mathematics;

namespace Utils
{
    public class MathUtils
    {
        public static float2 Remap(float2 value, float2 low1, float2 high1, float2 low2, float2 high2)
        {
            return low2 + (value - low1) * (high2 - low2) / (high1 - low1); 
        }
    }
}