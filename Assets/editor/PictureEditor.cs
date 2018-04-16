using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;


[CustomEditor(typeof(Picture))]
public class PictureEditorInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        Picture pic = (Picture)target;

        if (pic.transform.hasChanged)
            UpdatePackData();
    }

    void UpdatePackData()
    {
        Picture pic = (Picture)target;
        if (pic.transform.parent == null) return;
        Animation parent = pic.transform.parent.GetComponent<Animation>();
        if (parent == null) return;

        parent.OnChildTransform(pic.gameObject);
    }
}
