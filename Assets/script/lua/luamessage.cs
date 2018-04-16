using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

class luamessage
{
    static Package currentPackage;
    static GameAssets gameAssets;
    static List<PackerRect> PictureRects = new List<PackerRect>();
    static List<PackerRect> AnimationRects = new List<PackerRect>();

    public static void new_package_start(string packname, string filename)
    {
        string objName = "Package:" + packname;
        var root = GameObject.Find(objName);
        if (root)
            GameObject.DestroyImmediate(root);
        root = new GameObject(objName);
        PictureRects.Clear();
        AnimationRects.Clear();

        currentPackage = root.AddComponent<Package>();
        currentPackage.Name = packname;
        currentPackage.Filepath = filename;

        gameAssets = GameObject.Find("VM").GetComponent<GameAssets>();
    }

    public static void new_picture(int id, string pack)
    {
        ST_picture_part[] packs = JsonHelper.FromJson<ST_picture_part>(pack);

        Transform root = currentPackage.transform.Find("pictures");
        if (!root)
        {
            root = new GameObject("pictures").transform;
            root.parent = currentPackage.transform;
        }

        GameObject po = new GameObject("p"+id.ToString());
        po.transform.parent = root;
        Picture obj = po.AddComponent<Picture>();
        obj.AddPacks(id, packs, gameAssets);

        PictureRects.Add(obj.AABB);
        
        currentPackage.AddPicture(id, obj);
    }

    public static void new_animation(string pack)
    {
        ST_animation ani = JsonHelper.SimpleFromJson<ST_animation>(pack);
        int id = ani.id;
        string export = ani.export;

        Transform root = currentPackage.transform.Find("animations");
        if (!root)
        {
            root = new GameObject("animations").transform;
            root.parent = currentPackage.transform;
        }

        string name = "a" + id.ToString();
        if (!string.IsNullOrEmpty(export))
            name = name + "_" + export;
        GameObject ao = new GameObject(name);
        ao.transform.parent = root;
        Animation obj = ao.AddComponent<Animation>();
        currentPackage.AddAnimation(id, obj, ani);

        obj.SetPack(currentPackage, id);

        AnimationRects.Add(obj.AABB);
    }

    public static void sort_rects(List<PackerRect> rects, int dx=0, int dy=0, float mx=1.0f, float my=1.0f)
    {
        rects.Sort((a, b) => b.Area.CompareTo(a.Area));
        var packer = new RectanglePacker();

        for (int i = 0; i < rects.Count; ++i)
        {
            var rect = rects[i];
            int x, y;
            if (!packer.Pack((int)rect.w, (int)rect.h, out x, out y))
                throw new Exception("Uh oh, we couldn't pack the rectangle :(");
            rect.x = x;
            rect.y = y;

            var p = currentPackage.QueryComponent(rect.ID);
            int [] m = {1024, 0, 0, 1024, (int)((x + dx) * 16 * mx), (int)((y + dy) * 16 * my)};
            Matrix4x4 mat = Matrix.ToMatrix4x4(m);
            Matrix.SetTransformFromMatrix(p.transform, ref mat);
        }
    }

    public static void new_package_end()
    {
        sort_rects(PictureRects, -1000, -1000, 1, 1);
        sort_rects(AnimationRects);

        currentPackage.RefreshAniObj();
    }
}
