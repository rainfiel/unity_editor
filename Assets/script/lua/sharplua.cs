using System;
using System.Runtime.InteropServices;

using UnityEngine;

class MonoPInvokeCallbackAttribute : System.Attribute {
	public MonoPInvokeCallbackAttribute( Type t ) {}
}

class SharpLua {
	public enum var_type {
		NIL = 0,
		INTEGER = 1,
		INT64 = 2,
		REAL = 3,
		BOOLEAN = 4,
		STRING = 5,
		POINTER = 6,
		LUAOBJ = 7,
		SHARPOBJ = 8,
        SIMPLETABLE = 9,
	};
  //  [StructLayout(LayoutKind.Sequential)]
    public struct var {
   //     [MarshalAs(UnmanagedType.SysInt)]
        public var_type type;
		public int d;
		public long d64;
		public double f;
		public IntPtr ptr;
	};

    public static string VarToString(var v)
    {
        if (v.type == var_type.STRING || v.type == var_type.SIMPLETABLE)
        {
            return Marshal.PtrToStringAnsi(v.ptr);
        }
        else if (v.type == var_type.INTEGER)
        {
            return v.d.ToString();
        }
        else if (v.type == var_type.INT64)
        {
            return v.d64.ToString();
        }
        else if (v.type == var_type.REAL)
        {
            return v.f.ToString();
        }
        else if (v.type == var_type.BOOLEAN)
        {
            return v.d == 1 ? "true" : "false";
        }
        else if (v.type == var_type.NIL)
        {
            return "nil";
        }
        else if (v.type == var_type.SHARPOBJ)
        {
            return "SHARPOBJ+" + v.d.ToString();
        }
        else if (v.type == var_type.LUAOBJ)
        {
            return "LUAOBJ+" + v.d.ToString();
        }
        else
        {
            return v.ToString();
        }
    }
    public static object VarValue(var v)
    {
        switch (v.type)
        {
            case var_type.NIL:
                return null;
            case var_type.INTEGER:
                return v.d;
            case var_type.INT64:
                return v.d64;
            case var_type.REAL:
                return v.f;
            case var_type.BOOLEAN:
                return (v.d != 0) ? true : false;
            case var_type.STRING:
            case var_type.SIMPLETABLE:
                // todo: encoding
                return Marshal.PtrToStringAnsi(v.ptr);
            case var_type.POINTER:
                return v.ptr;
            case var_type.LUAOBJ:
                return new LuaObject { id = v.d };
            case var_type.SHARPOBJ:
                object ret = objects.Get(v.d);
                if (ret == null)
                {
                    throw new ArgumentException("Invalid sharp object");
                }
                return ret;
        }
        throw new ArgumentException("Invalid var");
    }

    public struct LuaObject {
		public int id;
	};

#if UNITY_IOS
    const string DLL = "__Internal";
#else
    const string DLL = "RPlugin";
#endif
    const int max_args = 256;	// Also defined in sharplua.c MAXRET

	IntPtr L;
	var[] args = new var[max_args];
	string[] strs = new string[max_args];
	static SharpObject objects = new SharpObject();	// All the SharpLua class share one one objects map

	public delegate string SharpFunction(int n, var[] argv);

    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr c_newvm([MarshalAs(UnmanagedType.LPStr)] string name, string directory, Callback cb, out IntPtr err);
    [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr c_newvm_buffer([MarshalAs(UnmanagedType.LPStr)] string main, int mainsize, string init, int initsize, string directory, Callback cb, out IntPtr err);
    [DllImport (DLL, CallingConvention=CallingConvention.Cdecl)]
	static extern void c_closevm(IntPtr L);
	[DllImport (DLL, CallingConvention=CallingConvention.Cdecl)]
	static extern IntPtr c_getglobal(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string key, out int id);
	[DllImport (DLL, CallingConvention=CallingConvention.Cdecl)]
	static extern int c_callfunction(IntPtr L, int argc, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeConst=max_args)] var[] argv,
		int strc, [In, MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.LPStr, SizeParamIndex=3)] string[] strs);
	[DllImport (DLL, CallingConvention=CallingConvention.Cdecl)]
	static extern int c_collectgarbage(IntPtr L, int n, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] int[] result);
	[DllImport (DLL, CallingConvention=CallingConvention.Cdecl)]
	static extern int c_pushstring(IntPtr ud, [MarshalAs(UnmanagedType.LPStr)] string str);

	delegate int Callback(int argc, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeConst=max_args)] var[] argv, IntPtr sud);
	[MonoPInvokeCallback (typeof (Callback))]
	static int CallSharp(int argc, [In, Out, MarshalAs(UnmanagedType.LPArray, SizeConst=max_args)] var[] argv, IntPtr sud) {
		try {
			SharpFunction f = (SharpFunction)objects.Get(argv[0].d);
			string ret = f(argc, argv);
			if (ret != null) {
				// push string into L for passing C sharp string to lua.
				if (c_pushstring(sud, ret) == 0) {
					throw new ArgumentException("Push string failed");
				}
			}
			return (int)argv[0].type;
		} catch (Exception ex) {
			c_pushstring(sud, ex.ToString());
            Debug.LogError(ex.ToString());
			return -1;
		}
	}

    public SharpLua(string name)
    {
        IntPtr err;
        IntPtr tmp = c_newvm(name, Application.dataPath, CallSharp, out err);
        if (err != IntPtr.Zero)
        {
            string msg = Marshal.PtrToStringAnsi(err);
            c_closevm(tmp);
            throw new ArgumentException(msg);
        }
        else
        {
            L = tmp;
        }
    }

    public SharpLua(string main, string init)
    {
        IntPtr err;
        IntPtr tmp = c_newvm_buffer(main, main.Length, init, init.Length, Application.dataPath, CallSharp, out err);
        if (err != IntPtr.Zero)
        {
            string msg = Marshal.PtrToStringAnsi(err);
            c_closevm(tmp);
            throw new ArgumentException(msg);
        }
        else
        {
            L = tmp;
        }
    }

    ~SharpLua() {
		c_closevm(L);
	}

	public void Close() {
		c_closevm(L);
		L = IntPtr.Zero;
	}

	public LuaObject GetFunction(string name) {
		int id;
		IntPtr err = c_getglobal(L, name, out id);
		if (id != 0) {
			// succ 
			return new LuaObject { id = id };
		} else {
			if (err == IntPtr.Zero) {
				return new LuaObject();	// nil
			} else {
				throw new ArgumentException(Marshal.PtrToStringAnsi(err));
			}
		}
	}

	int pushvalue(ref var v, object arg) {
		if (arg == null) {
			v.type = (int)var_type.NIL;
		} else {
			Type t = arg.GetType();
			if (t == typeof(int)) {
				v.type = var_type.INTEGER;
				v.d = (int)arg;
			} else if ( t == typeof(long)) {
				v.type = var_type.INT64;
				v.d64 = (long)arg;
			} else if (t == typeof(float)) {
				v.type = var_type.REAL;
				v.f = (float)arg;
			} else if (t == typeof(double)) {
				v.type = var_type.REAL;
				v.f = (double)arg;
			} else if (t == typeof(bool)) {
				v.type = var_type.BOOLEAN;
				v.d = (bool)arg ? 1 : 0;
			} else if (t == typeof(string)) {
				v.type = var_type.STRING;
				return 2;	// string
			} else if (t == typeof(LuaObject)) {
				v.type = var_type.LUAOBJ;
				v.d = ((LuaObject)arg).id;
			} else if (t.IsClass) {
				v.type = var_type.SHARPOBJ;
				v.d = objects.Query(arg);
			} else {
				return 0;	// error
			}
		}
		return 1;
	}

	public void CollectGarbage() {
		const int cap = 256;
		int[] result = new int[cap];	// 256 per cycle
		int n;
		do {
			n = c_collectgarbage(L, cap, result);
			for (int i=0;i<n;i++) {
				objects.Remove(result[i]);
			}
		} while(n<cap && n > 0);
	}

	public object[] CallFunction(LuaObject func, params object[] arg) {
		int n = arg.Length;
		if (n+1 > max_args) {
			throw new ArgumentException("Too many args");
		}
		args[0].type = var_type.LUAOBJ;
		args[0].d = func.id;

		int sn = 0;
		for (int i = 0; i < n; i++) {
			int r = pushvalue(ref args[i+1], arg[i]);
			switch(r) {
			case 0:
				throw new ArgumentException(String.Format("Unsupport type : {1} at {0}", i, arg[i].GetType()));
			case 1:
				break;
			case 2:
				// string
				args[i+1].d = sn;
				strs[sn] = (string)arg[i];
				++sn;
				break;
			}
		}
		int retn = c_callfunction(L, n+1, args, sn, strs);
		if (retn < 0) {
			throw new ArgumentException(VarToString(args[0]));
		}
		if (retn == 0) {
			return null;
		}
		object[] ret = new object[retn];
		for (int i = 0; i < retn; i++) {
            ret[i] = VarValue(args[i]);
		}
		
		return ret;
	}
};
