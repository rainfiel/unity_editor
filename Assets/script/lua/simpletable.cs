using System;
using UnityEngine;

public static class JsonHelper
{
    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static T SimpleFromJson<T>(string json)
    {
       return JsonUtility.FromJson<T>(json);
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    public static string ToJson<T>(T[] array, bool prettyPrint)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper, prettyPrint);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }
}

[Serializable]
public class ST_picture_part
{
    public int tex;
    public string tex_name;
    public float[] src;
    public float[] screen;
}

[Serializable]
public class ST_component
{
    public int id=-1;
    public string name = string.Empty;
}

[Serializable]
public class ST_frame_part
{
    public int index;
    public int[] mat;
    public uint color=0xFFFFFFFF;
    public uint add =0;
    public bool touch=false;
}

[Serializable]
public class ST_frame
{
    public ST_frame_part[] parts;
}

[Serializable]
public class ST_action
{
    public string name = "default";
    public ST_frame[] frames;
}

[Serializable]
public class ST_animation
{
    public int id;
    public string export;
    public string type;
    public ST_component[] component;
    public ST_action[] actions;
}
