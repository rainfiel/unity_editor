
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(GameAssets))]
public class GameAssetsEditor : Editor
{
    string AssetPath = "Assets/Resources/res/asset";
    public override void OnInspectorGUI()
    {
        GameAssets ga = (GameAssets)target;
        if (!GameSpecified())
        {
            if (GUILayout.Button("选择游戏"))
            {
                SelectGame();
            }
            return;
        }
        else
        {
            GUILayout.Label("当前游戏: " + ga.GameName);
            GUILayout.Label("当前目录: " + ga.GameFolder);
            GUILayout.Label("* 删除" + AssetPath + "以重新选择游戏");
        }

        GUILayout.Space(20);

        if (GUILayout.Button("刷新资源"))
        {
            ga.AllCocoFiles();
        }

        if (ga.PackagePairs != null && ga.PackagePairs.Length > 0)
        {
            for (int i = 0; i < ga.PackagePairs.Length; i++)
            {
                PackagePair pp = ga.PackagePairs[i];
                GUILayout.BeginHorizontal();
                pp.Obj = (GameObject)EditorGUILayout.ObjectField(pp.Name, pp.Obj, typeof(GameObject), false);
                if (pp.Obj == null)
                {
                    if (GUILayout.Button("创建"))
                        ga.CreatePackage(pp);
                } else
                {
                    if (GUILayout.Button("保存"))
                    {
                        var p = pp.Obj.GetComponent<Package>();
                        ga.SavePackage(p);
                    }
                }
                GUILayout.EndHorizontal();
            }
        } else
        {
            GUILayout.Label("找不到ejoy2d资源包");
        }
    }

    bool GameSpecified()
    {
        GameAssets ga = (GameAssets)target;
        return !string.IsNullOrEmpty(ga.GameName) && !string.IsNullOrEmpty(ga.GameFolder)
            && Directory.Exists(AssetPath);
    }

    void SelectGame()
    { 
        string gameFolderPath = EditorUtility.OpenFolderPanel("Select Game Folder", Application.dataPath+"/../../../project/", "");

        // Cancelled dialog
        if (string.IsNullOrEmpty(gameFolderPath))
            return;

        if (!gameFolderPath.Contains("ejoy2dx/project"))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个ejoy2dx工程(ejoy2dx/project/GAME_NAME)", "确定");
            return;
        }
        
        string gameFolderName = gameFolderPath.Split(new char[] { '/', '\\' }).LastOrDefault();
        if (!gameFolderPath.Contains("ejoy2dx/project/"+gameFolderName))
        {
            EditorUtility.DisplayDialog("错误", "请选择一个ejoy2dx工程(ejoy2dx/project/GAME_NAME)", "确定");
            return;
        }

        string targetPath = AssetPath;
        
        if (Directory.Exists(targetPath))
        {
            EditorUtility.DisplayDialog("错误", "Assets/Resources/res目录下已经存在一个工程，请删除后再试", "确定");
            return;
        }

        GameAssets ga = (GameAssets)target;
        ga.SetGame(gameFolderPath, gameFolderName);

#if UNITY_EDITOR_WIN
        Process cmd = Process.Start("CMD.exe", string.Format("/C mklink /J \"{0}\" \"{1}/asset\"", targetPath, gameFolderPath));
        cmd.WaitForExit();
#elif UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX
			// @todo
#endif
        
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

}
