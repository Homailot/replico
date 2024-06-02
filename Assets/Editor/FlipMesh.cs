using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


public static class FlipMesh
{
    [MenuItem("CONTEXT/MeshFilter/Flip Z")]
    public static void FlipZ(MenuCommand command)
    {
        var filter = (MeshFilter) command.context;
        FlipObj(filter.sharedMesh, false, false, true);
    }
    
    [MenuItem("CONTEXT/MeshFilter/Flip X")]
    public static void FlipX(MenuCommand command)
    {
        var filter = (MeshFilter) command.context;
        FlipObj(filter.sharedMesh, true, false, false);
    }
    
    [MenuItem("CONTEXT/MeshFilter/Save as new asset")]
    public static void SaveAsNewAsset(MenuCommand command)
    {
        var filter = (MeshFilter) command.context;
        var mesh = filter.sharedMesh;
        SaveMesh(mesh, mesh.name, true, true);
    }

    private static void SaveMesh (Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
        var path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path)) return;
        
        path = FileUtil.GetProjectRelativePath(path);

        var meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
        
        if (optimizeMesh)
             MeshUtility.Optimize(meshToSave);
        
        AssetDatabase.CreateAsset(meshToSave, path);
        AssetDatabase.SaveAssets();
    }
    
    private static void FlipObj(Mesh mesh, bool flipX, bool flipY, bool flipZ)
    {
        var vertices = mesh.vertices;
        for (var i = 0; i < vertices.Length; i++)
        {
            var c = vertices[i];
            if (flipX) c.x *= -1;
            if (flipY) c.y *= -1;
            if (flipZ) c.z *= -1;
            vertices[i] = c;
        }

        mesh.vertices = vertices;
        if (flipX ^ flipY ^ flipZ) FlipNormals(mesh);
    }
    
    private static void FlipNormals(Mesh mesh)
    {
        var tris = mesh.triangles;
        for (var i = 0; i < tris.Length / 3; i++)
        {
            var a = tris[i * 3 + 0];
            var b = tris[i * 3 + 1];
            var c = tris[i * 3 + 2];
            tris[i * 3 + 0] = c;
            tris[i * 3 + 1] = b;
            tris[i * 3 + 2] = a;
        }
        mesh.triangles = tris;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
