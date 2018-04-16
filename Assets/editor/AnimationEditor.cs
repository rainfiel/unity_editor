using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(Animation))]
public class AnimationEditorInspector : Editor
{
    void NumberArray(int num, out string[] names, out int[] numbers)
    {
        names = new string[num];
        numbers = new int[num];
        for (int i=0; i < num; i++)
        {
            names[i] = i.ToString();
            numbers[i] = i;
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        Animation ani = (Animation)target;
        
        if (ani.ActionCount > 1)
        {
            int[] numbers;
            string[] names;
            NumberArray(ani.ActionCount, out names, out numbers);
            ani.ActionIndex = EditorGUILayout.IntPopup("选择动作", 0, names, numbers);
        }

        if (ani.ActionFrameCount > 1)
        {
            bool auto = EditorGUILayout.Toggle("自动播放", ani.AutoPlay);
            if (ani.AutoPlay != auto)
                ani.AutoPlay = auto;

            if (!ani.AutoPlay)
            {
                int[] numbers;
                string[] names;
                NumberArray(ani.ActionFrameCount, out names, out numbers);
                int frame = EditorGUILayout.IntSlider(ani.FrameIndex, 0, ani.ActionFrameCount);
                //int frame = EditorGUILayout.IntPopup("选择帧", 0, names, numbers);
                if (frame != ani.FrameIndex)
                {
                    ani.FrameIndex = frame;
                    ani.AniStep();
                }
            }
        }

        if (GUILayout.Button("刷新"))
            ani.AniStep();


        if (ani.transform.hasChanged)
            UpdatePackData();
    }

    void UpdatePackData()
    {
        Animation ani = (Animation)target;
        if (ani.transform.parent == null) return;
        Animation parent = ani.transform.parent.GetComponent<Animation>();
        if (parent == null) return;

        parent.OnChildTransform(ani.gameObject);
    }
}
