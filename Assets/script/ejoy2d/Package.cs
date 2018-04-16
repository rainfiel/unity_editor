using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Package : MonoBehaviour {
    public string Name;
    public string Filepath;

    public Dictionary<int, Picture> Pictures = new Dictionary<int, Picture>();
    public Dictionary<int, Animation> Animations = new Dictionary<int, Animation>();

    public List<ST_animation> AnimationPacks = new List<ST_animation>();
    public Dictionary<int, ST_animation> AnimationMap = new Dictionary<int, ST_animation>();

    public Dictionary<int, List<Animation>> AllAnimations = new Dictionary<int, List<Animation>>();

    public void AddPicture(int id, Picture pic)
    {
        Pictures[id] = pic;
    }

    public void AddAnimation(int id, Animation ani, ST_animation pack)
    {
        Animations[id] = ani;
        AnimationPacks.Add(pack);
        AnimationMap[id] = pack;
    }

    public ST_animation GetAnimationPack(int id)
    {
        return AnimationMap[id];
    }

    public MonoBehaviour QueryComponent(int id)
    {
        if (Pictures.ContainsKey(id))
            return Pictures[id];
        if (Animations.ContainsKey(id))
            return Animations[id];
        return null;
    }

    /*
    public void Save()
    {
        string data = JsonHelper.ToJson<ST_animation>(AnimationPacks.ToArray());

        VM.Inst.L.CallLua("save_package", Name, Filepath, data);
    }
    */

    public string PackageString()
    {
        return JsonHelper.ToJson<ST_animation>(AnimationPacks.ToArray()); 
    }

    public void RefreshAniObj()
    {
        Animation[] anis = gameObject.GetComponentsInChildren<Animation>();
        AllAnimations.Clear();

        foreach (var ani in anis)
        {
            if (!AllAnimations.ContainsKey(ani.AniPackID))
                AllAnimations[ani.AniPackID] = new List<Animation>();
            AllAnimations[ani.AniPackID].Add(ani);
        }
    }

    public void OnPackageChanged(int packID)
    {
        if (!AllAnimations.ContainsKey(packID)) return;
        List<Animation> anis = AllAnimations[packID];
        foreach (var ani in anis)
            ani.AniStep();
    }
}
