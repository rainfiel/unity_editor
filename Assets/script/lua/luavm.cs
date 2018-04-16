using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Reflection;

public class LuaVM
{
    SharpLua L;
    public bool Started = false;
    public LuaVM(string main, string init)
    {
        L = new SharpLua(main, init);
        
        SharpLua.LuaObject set_print = L.GetFunction("set_print");
        SharpLua.SharpFunction sharpPrint = LuaPrint;
        L.CallFunction(set_print, sharpPrint);

        SharpLua.LuaObject loader = L.GetFunction("unity_res_load");
        SharpLua.SharpFunction luaLoader = LuaLoader;
        L.CallFunction(loader, luaLoader);
    }
    
    public void Start(params object[] args)
    {
        Started = true;
        SharpLua.LuaObject init = L.GetFunction("init");
        L.CallFunction(init);

        SharpLua.LuaObject message = L.GetFunction("set_call_sharp");
        SharpLua.SharpFunction sharpMsg = LuaCaller;
        L.CallFunction(message, sharpMsg);

        SharpLua.LuaObject start = L.GetFunction("start");
        L.CallFunction(start, args);
    }

    public void Update()
    {
        if (!Started) return;

        SharpLua.LuaObject update = L.GetFunction("update");
        L.CallFunction(update, Time.deltaTime);
    }

    public void FixedUpdate()
    {
        if (!Started) return;

        SharpLua.LuaObject fixedupdate = L.GetFunction("fixedupdate");
        L.CallFunction(fixedupdate, Time.fixedDeltaTime);
    }

    public void CallLua(params object[] args)
    {
        SharpLua.LuaObject input = L.GetFunction("call_by_sharp");
        L.CallFunction(input, args);
    }

    public void GC()
    {
        SharpLua.LuaObject gc = L.GetFunction("collectgarbage");
        L.CallFunction(gc, "collect");
        L.CollectGarbage();
    }

    public void Close()
    {
        Started = false;
        SharpLua.LuaObject close = L.GetFunction("close");
        L.CallFunction(close);

        L.Close();
        L = null;
    }
    
    #region lua->csharp
    static string LuaPrint(int n, SharpLua.var[] argv)
	{
        string info = "";
        for (int i = 1; i < n; i++)
            info += SharpLua.VarToString(argv[i])+"\t";
		Debug.Log(info);
		return null;
	}

    static string LuaLoader(int n, SharpLua.var[] argv)
    {
        if (argv.Length < 2)
            return null;
        string path = SharpLua.VarToString(argv[1]);
        if (string.IsNullOrEmpty(path))
            return null;

        TextAsset script = Resources.Load(path) as TextAsset;
        if (script == null)
            return null;

        argv[0].type = SharpLua.var_type.STRING;
        return script.ToString();
    }
    
    static string LuaCaller(int n, SharpLua.var[] argv)
    {
        string type = SharpLua.VarToString(argv[1]);

        //TODO NO Reflection
        Type MessageMgr = Assembly.GetExecutingAssembly().GetType("luamessage");
        MethodInfo theMethod = MessageMgr.GetMethod(type);

        if (theMethod == null)
        {
            Debug.LogError("LuaCaller failed, no handler found:" + type);
            return null;
        }

        object[] param = null;
        if (n > 2)
        {
            param = new object[n - 2];
            for (int i=2; i<n; i++)
            {
                param[i - 2] = SharpLua.VarValue(argv[i]);
            }
        }

        try
        {
            theMethod.Invoke(null, param);
        } catch (Exception ex)
        {
            Debug.LogError("LuaCaller invoke failed:" + ex.ToString());
            return null;
        }

        return null;
    }
    #endregion
}
