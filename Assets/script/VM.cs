using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class VM : MonoBehaviour {
    [HideInInspector]
    public LuaVM L;
    public string StartScriptPath;
    public string InitScriptPath;
//    public string IP;
//    public int LoginPort;
//    public int GamePort;
//    public string UserName;

    [HideInInspector]
    public bool IsOffline;

    public static VM Inst;
	// Use this for initialization
	void Start () {
        Inst = this;

        Invoke("Launch", 0.5f);
    }

    void OnDestroy()
    {
        Close();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsStarted)
            L.Update();
    }

    void FixedUpdate()
    {
        if (IsStarted)
            L.FixedUpdate();
    }

    public bool IsStarted
    {
        get
        {
            return L != null && L.Started;
        }
        set
        {
            if (L != null)
                L.Started = value;
        }
    }

    public void Launch()
    {
        TextAsset mainScript = Resources.Load(StartScriptPath) as TextAsset;
        string mainText = mainScript.ToString();
        
        TextAsset initScript = Resources.Load(InitScriptPath) as TextAsset;
        string initText = initScript.ToString();
        
        L = new LuaVM(mainText, initText);
        L.Start();
    }

    public void Close()
    {
        if (L != null)
        {
            L.Close();
            L = null;
        }
    }

    public void Pause()
    {
        IsStarted = false;
    }
	
}
