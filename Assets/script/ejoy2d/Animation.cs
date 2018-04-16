using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Animation : MonoBehaviour {
   // [HideInInspector]
    public int AniPackID;
    public int ActionIndex = 0;
    public int FrameIndex = 0;
    public PackerRect AABB;

    [HideInInspector]
    public Package OwnerPackage;
    public ST_animation AniPack
    {
        get
        {
            return OwnerPackage.GetAnimationPack(AniPackID);
        }
    }

    public int ActionCount
    {
        get
        {
            return AniPack.actions.Length;
        }
    }

    public ST_action CurrentAction
    {
        get
        {
            return AniPack.actions[ActionIndex];
        }
    }

    public int ActionFrameCount
    {
        get
        {
            return CurrentAction.frames.Length;
        }
    }

    bool _AutoPlay = false;
    public bool AutoPlay
    {
        set {
            FrameIndex = 0;
            _AutoPlay = value;
        }
        get { return _AutoPlay; }
    }
    
//    [HideInInspector]
    public List<GameObject> Components = null;
    void Update()
    {
        if (_AutoPlay)
        {
            AniStep();

            FrameIndex++;
            ST_action action = AniPack.actions[ActionIndex];
            if (FrameIndex < 0 || FrameIndex >= action.frames.Length)
                FrameIndex = 0;
        }
    }
    
    List<GameObject> VisibleInFrame = new List<GameObject>();
    public void AniStep()
    {
        if (Components == null) return;
        ST_action action = CurrentAction;
        if (FrameIndex < 0 || FrameIndex >= action.frames.Length)
            return;

        ST_frame frame = action.frames[FrameIndex];
        VisibleInFrame.Clear();
        for (int i = 0; i < frame.parts.Length; i++)
        {
            ST_frame_part part = frame.parts[i];
            if (part.index >= 0 && part.index < Components.Count)
            {
                GameObject obj = Components[part.index];
                VisibleInFrame.Add(obj);
                obj.SetActive(true);
                var mat = Matrix.ToMatrix4x4(part.mat, -i*1.0f);
                Matrix.SetTransformFromMatrix(obj.transform, ref mat);
            }
            else
            {
                //Debug.LogError("components out of range");
            }
        }
        if (VisibleInFrame.Count != Components.Count)
        {
            foreach (var obj in Components)
            {
                if (!VisibleInFrame.Contains(obj))
                {
                    obj.SetActive(false);
                }
            }
        }
    }
    
    public void SetPack(Package package, int packID)
    {
        AniPackID = packID;
        OwnerPackage = package;
        var pack = AniPack;

        int num = pack.component.Length;
        float[] minx = new float[num];
        float[] miny = new float[num];
        float[] maxx = new float[num];
        float[] maxy = new float[num];

        Components = new List<GameObject>();
        for (int i=0; i<num; i++)
        {
            ST_component comp = pack.component[i];
            if (comp.id >= 0)
            {
                var obj = package.QueryComponent(comp.id);
                var child = GameObject.Instantiate(obj.gameObject);
                child.transform.parent = transform;
                Components.Add(child);

                PackerRect r;
                var p = obj.gameObject.GetComponent<Picture>();
                if (p)
                    r = p.AABB;
                else
                {
                    var a = obj.gameObject.GetComponent<Animation>();
                    if (a) 
                        r = a.AABB;
                    else
                        r = PackerRect.Default;
                }

                minx[i] = r.x;
                miny[i] = r.y;
                maxx[i] = r.w;
                maxy[i] = r.h;
            }
        }
        AABB.x = Mathf.Min(minx);
        AABB.y = Mathf.Min(miny);
        AABB.w = Mathf.Max(maxx);
        AABB.h = Mathf.Max(maxy);
        AABB.ID = packID;
        AniStep();
    }
    
    public void OnChildTransform(GameObject obj)
    {
        if (!obj.activeInHierarchy) return;

        int index = Components.IndexOf(obj);
        if (index < 0) return;

        int[] mat = Matrix.TransformToMatrix2x3(obj.transform);
        
        ST_frame frame = CurrentAction.frames[FrameIndex];
        frame.parts[index].mat = mat;

        OwnerPackage.OnPackageChanged(AniPackID);
    /*    string x = "";
        foreach (int i in mat)
            x += " " + i.ToString();
        Debug.Log(x);*/
    }
}
