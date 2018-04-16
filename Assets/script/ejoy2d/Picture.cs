using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Picture : MonoBehaviour {
    public ST_picture_part[] PictureParts;
    public int ID;
    public PackerRect AABB;

    public void AddPacks(int id, ST_picture_part[] packs, GameAssets ga)
    {
        ID = id;
        PictureParts = packs;
        foreach (var p in packs)
        {
            AddPack(p, ga);
        }
        UpdateAABB();
    }

    void UpdateAABB()
    {
        var filters = GetComponents<MeshFilter>();

        float[] minx = new float[filters.Length];
        float[] miny = new float[filters.Length];
        float[] maxx = new float[filters.Length];
        float[] maxy = new float[filters.Length];
        for (int i=0; i < filters.Length; i++)
        {
            var mesh = filters[i].sharedMesh;
            var bound = mesh.bounds;
            minx[i] = bound.min.x;
            miny[i] = bound.min.y;
            maxx[i] = bound.max.x;
            maxy[i] = bound.max.y;
        }
        AABB.x = Mathf.Min(minx);
        AABB.y = Mathf.Min(miny);
        AABB.w = Mathf.Max(maxx) - AABB.x;
        AABB.h = Mathf.Max(maxy) - AABB.y;
        AABB.ID = ID;
    }
    
    public void AddPack(ST_picture_part pack, GameAssets ga)
    {
        var render = gameObject.AddComponent<MeshRenderer>();
        render.sharedMaterial = ga.GetMaterial(pack.tex_name);
        
        var filter = gameObject.AddComponent<MeshFilter>();
        filter.mesh = NewMesh(pack, render.sharedMaterial.mainTexture);
    }
    
    Mesh NewMesh(ST_picture_part pack, Texture tex)
    {
        var mesh = new Mesh();
        
        var vert = new Vector3[4];
        vert[0] = new Vector3(pack.screen[0] / 16, -pack.screen[1] / 16, 0);
        vert[1] = new Vector3(pack.screen[2] / 16, -pack.screen[3] / 16, 0);
        vert[2] = new Vector3(pack.screen[4] / 16, -pack.screen[5] / 16, 0);
        vert[3] = new Vector3(pack.screen[6] / 16, -pack.screen[7] / 16, 0);
        mesh.vertices = vert;

        var tri = new int[6];
        tri[0] = 0;
        tri[1] = 3;
        tri[2] = 1;

        tri[3] = 2;
        tri[4] = 1;
        tri[5] = 3;
        mesh.triangles = tri;

        var normals = new Vector3[4];
        normals[0] = -Vector3.forward;
        normals[1] = -Vector3.forward;
        normals[2] = -Vector3.forward;
        normals[3] = -Vector3.forward;
        mesh.normals = normals;
        
        Vector2[] uv = new Vector2[4];
        uv[0] = new Vector2(pack.src[0] / tex.width, 1 - pack.src[1] / tex.height);  // 0
        uv[1] = new Vector2(pack.src[2] / tex.width, 1 - pack.src[3] / tex.height);  // 1
        uv[2] = new Vector2(pack.src[4] / tex.width, 1 - pack.src[5] / tex.height);  // 2
        uv[3] = new Vector2(pack.src[6] / tex.width, 1 - pack.src[7] / tex.height);  // 3
        mesh.uv = uv;

        mesh.RecalculateBounds();
        return mesh;
    }
}
