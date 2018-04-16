using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(Package))]
public class PackageEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

    /*    if (GUILayout.Button("保存"))
        {
            DoSave((Package)target);
        }*/
    }
    
}
