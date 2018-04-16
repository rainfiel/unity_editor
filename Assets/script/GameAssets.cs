using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[SerializeField]
public class PackagePair
{
    public string Name;
    public GameObject Obj;
    public PackagePair(string name)
    {
        Name = name;
        Obj = null;
    }

    public string UnityPath()
    {
        return string.Format("res/asset/{0}", Name);
    }

    public string GameObjectName()
    {
        return string.Format("Package:{0}", Name);
    }
}

public class GameAssets : MonoBehaviour {
    public string GameFolder;
    public string GameName;
    public string AssetFolder;

    public PackagePair[] PackagePairs;
    public Dictionary<string, Material> MaterialRes = new Dictionary<string, Material>();

    public VM _vmInst;
    public VM VMInst
    {
        get
        {
            if (_vmInst == null || !_vmInst.IsStarted)
            {
                GameObject go = GameObject.Find("VM");
                _vmInst = go.GetComponent<VM>();
                if (!_vmInst.IsStarted)
                    _vmInst.Launch();
            }
            return _vmInst;
        }
    }

    public void SetGame(string path, string name)
    {
        GameFolder = path;
        GameName = name;
        AssetFolder = GameFolder + "/asset";

        AllCocoFiles();
    }
    
    public void AllCocoFiles()
    {
        DirectoryInfo dir = new DirectoryInfo(AssetFolder);
        FileInfo[] info = dir.GetFiles("*.coco.bytes");
        
        PackagePairs = new PackagePair[info.Length];
        for (int i = 0; i < info.Length; i++)
        {
            FileInfo f = info[i];
            string name = f.FullName.Split(new char[] { '/', '\\' }).LastOrDefault();
            name = name.Substring(0, name.Length - 11);
            PackagePairs[i] = new PackagePair(name);
            PackagePairs[i].Obj = GameObject.Find(PackagePairs[i].GameObjectName());
        }
    }

    public Material GetMaterial(string texName)
    {
        string matName = texName.Split(new char[] { '/', '\\' }).LastOrDefault();
        if (MaterialRes.ContainsKey(matName))
            return MaterialRes[matName];

        Material material = null;
        string matPath = string.Format("Assets/Resources/mat/{0}.mat", matName);
        Object mat = Resources.Load(matPath);
        if (mat != null)
        {
            material = (Material)mat;
        }
        else
        { 
            var tex = Resources.Load(texName) as Texture;

            material = new Material(Shader.Find("Mobile/Particles/Alpha Blended"));
            AssetDatabase.CreateAsset(material, matPath);
            material.mainTexture = tex;
        }

        MaterialRes[matName] = material;
        return material;
    }

    public string GamePath(string name)
    {
        return string.Format("{0}/asset/{1}", GameFolder, name);
    }

    public void CreatePackage(PackagePair pp)
    {
        VMInst.L.CallLua("load_package", pp.Name, pp.UnityPath());

        pp.Obj = GameObject.Find(pp.GameObjectName());
    }

    public void SavePackage(Package package)
    {
        VMInst.L.CallLua("save_package", package.Name, GamePath(package.Name), package.PackageString());
    }
}
