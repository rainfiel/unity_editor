
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(VM))]
public class VMEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        VM vm = (VM)target;
        
        if (!vm.IsStarted)
        {
            if (GUILayout.Button("Start VM"))
            {
                vm.Launch();
            }
        }
        else
        {
            if (GUILayout.Button("Shutdown VM"))
            {
                vm.Close();
            }
        }
    }
    
}
