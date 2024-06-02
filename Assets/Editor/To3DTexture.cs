using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class To3DTexture
{
    [MenuItem("Assets/To 3D Texture")]
    public static void ToTo3DTexture()
    {
        var textures = new List<Texture2D>();
        for (var i = 0; i < 64; i++)
        {
            var texture = Resources.Load<Texture2D>($"HDR_L_{i}");
            Debug.Log(texture.format);
            textures.Add(texture);
        }
        
        var texture3D = new Texture3D(64, 64, 64, TextureFormat.RFloat, false)
        {
            filterMode = FilterMode.Point
        };
        
        var colors = new Color[64 * 64 * 64];
        
        for (var i = 0; i < 64; i++)
        {
            var texture = textures[i] as Texture2D;
            var pixels = texture.GetPixels();
            for (var j = 0; j < 64; j++)
            {
                for (var k = 0; k < 64; k++)
                {
                    colors[i * 64 * 64 + j * 64 + k] = pixels[j * 64 + k];
                }
            }
        }
        
        texture3D.SetPixels(colors);
        texture3D.Apply();
        
        AssetDatabase.CreateAsset(texture3D, "Assets/Textures/3DTexture.asset");
    }
}