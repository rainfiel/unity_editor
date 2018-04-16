
//#define LOG_DEBUG

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

[InitializeOnLoad]
public class EditorPlayMode
{
    static EditorPlayMode()
    {
        EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
    }

    static void HandleOnPlayModeChanged()
    {
        if (EditorApplication.isPlaying)
            LuaFileImporter.SyncAll();
    }
}

class LuaFileImporter : AssetPostprocessor
{
    const string BytesSuffix = ".bytes";
    const string LuaSuffix = ".lua";
    const string CsvSuffix = ".csv";
    static string[] LuaPaths = new string[] { "Assets/Resources/lua" };
    static void OnPostprocessAllAssets(String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths)
    {
        for (int i=0; i < importedAssets.Length; i++)
        {
            string asset = importedAssets[i];
            ProcessTxtFile(asset);
        }
    }

    static void ProcessTxtFile(string path)
    {
        if (path.EndsWith(LuaSuffix, true, null))
        {
            string text = File.ReadAllText(path);
            string target = path.Substring(0, path.Length - LuaSuffix.Length) + BytesSuffix;

            File.WriteAllText(target, text);
        }
        if (path.EndsWith(CsvSuffix, true, null))
        {
            string text = File.ReadAllText(path);
            string target = path + BytesSuffix;
            File.WriteAllText(target, text);
        }
    }

    static void SyncPath(string path)
    {
        foreach (var file in Directory.GetFiles(path))
        {
            ProcessTxtFile(file);
        }
    }

    [MenuItem("rplugin/Lua Script/Sync All")]
    public static void SyncAll()
    {
        AssetDatabase.SaveAssets();

        foreach (var path in LuaPaths)
        {
            SyncPath(path);
        }

        AssetDatabase.Refresh(ImportAssetOptions.DontDownloadFromCacheServer | ImportAssetOptions.ForceSynchronousImport);
    }
}

/*
namespace InEditor.LuaPreprocess
{
	class LuaFileImporter : AssetPostprocessor
	{
		const string		LuaResSuffix		= ".bytes";
		const string		LuaSuffix			= ".lua";

		static readonly string		SourcePath;
		static readonly string		TargetPath;
		static readonly string		DatabasePath;

		static readonly Regex		PathRegex;

		static int					Depth;
		static int					SyncCount;

		static HashSet<string>		Macros;
		static string				MacrosHash;
		static BuildTarget			CurrentBuildTarget;

#if !EJOY_BUILD_LUA_IN_EDITOR
		static bool					InBuildPlayer { get { return EnterInBuildPlayer.InBuild; } }
#else
		const bool					InBuildPlayer = true;
#endif

		static System.Diagnostics.Process	cmdProcess = new System.Diagnostics.Process();
        static Regex						AuthorRegex = new Regex(@"^\s*\S+\s+(\S+)");
        static Dictionary<string, string>	LineToAnchor = new Dictionary<string, string>();

		public static readonly string[]			DefaultDefines = EditorUserBuildSettings.activeScriptCompilationDefines
																	.Where( define => define.StartsWith( "UNITY_" ) )
																	.Union( new string[] {
#if UNITY_ANDROID
																		"UNITY_ANDROID",
#elif UNITY_IOS
																		"UNITY_IOS",
#elif UNITY_STANDALONE_WIN
																		"UNITY_STANDALONE_WIN",
#else
#warning 不认识的平台
#endif
																		} )
																	.Except( new string[] {
																			"UNITY_PRO_LICENSE", "UNITY_TEAM_LICENSE", "UNITY_ASSERTIONS",
																		} )
																	.OrderBy( define => define )
																	.ToArray();


		class EnterInBuildPlayer
		{
			static readonly string		LockFilePath = Path.Combine( Path.GetDirectoryName( Application.dataPath ), "BuildFile.lock" );

			public static bool			InBuild { get { return File.Exists( LockFilePath ); } }

			FileStream		Stream;

			public EnterInBuildPlayer()
			{
				lock( LockFilePath )
				{
					Debug.Log( string.Format( "[build] create: {0}", LockFilePath ) );
					Stream = File.Open( LockFilePath, FileMode.Create, FileAccess.Write, FileShare.Read );
					using( var file = new StreamWriter( Stream ) )
					{
						file.Write( DateTime.Now );
						file.Flush();
					}
				}
			}

			~EnterInBuildPlayer()
			{
				Release();
			}

			public void Release()
			{
				lock( LockFilePath )
				{
					if( Stream != null )
					{
						Debug.Log( string.Format( "[build] delete: {0}", LockFilePath ) );
						Stream.Close();
						Stream = null;
						File.Delete( LockFilePath );
					}
				}
			}

			[MenuItem( "Generator/Lua Script/Clear InBuild Pref" )]
			static void ClearInBuildPref()
			{
				Debug.Log( LockFilePath );
				File.Delete( LockFilePath );
			}
		}

		static LuaFileImporter()
		{
			SourcePath = LuaSourceCodeRepo.BasePath;
			TargetPath = LuaPreprocessedRepo.BasePath;
			DatabasePath = Path.Combine( TargetPath, "database.json" );

			PathRegex = new Regex( @"^(.+?)\.[^\.]+$", RegexOptions.IgnoreCase );
        }

		static void OnPostprocessAllAssets( String[] importedAssets, String[] deletedAssets, String[] movedAssets, String[] movedFromAssetPaths )
		{
#if EJOY_BUILD_ASSETBUNDLE_APP
			return;
#endif
#if !EJOY_BUILD_LUA_IN_EDITOR
			if( InBuildPlayer )
				return;
#endif

			try
			{
				if( Depth++ == 0 )
					BuildScriptingDefines( out Macros, out MacrosHash, out CurrentBuildTarget );

				foreach( var path in deletedAssets )
					Deleted( path );

				for( var i = 0; i < movedAssets.Length; ++i )
				{
					Deleted( movedFromAssetPaths[ i ] );
					if( Sync( movedAssets[ i ], Macros, MacrosHash ) )
						++SyncCount;
                }

				foreach( var path in importedAssets )
					if( Sync( path, Macros, MacrosHash ) )
						++SyncCount;
            }
			finally
			{
				if( --Depth == 0 && SyncCount > 0 )
				{
					AssetDatabase.Refresh();

					var			msg = "sync lua script: " + SyncCount + " / " + Macros.StringJoin( ", " ) + " @ " + CurrentBuildTarget + ", " + InBuildPlayer;

					if( !InBuildPlayer )
						Debug.Log( msg );
					else
						Debug.LogWarning( msg );

					SyncCount = 0;
					Macros = null;
					MacrosHash = null;
					CurrentBuildTarget = 0;
				}
			}
		}

		static void BuildScriptingDefines( out HashSet<string> defines, out string hash, out BuildTarget target )
		{
			target = LuaPreprocessor.CurrentBuildTarget;
			defines = new HashSet<string>( LuaPreprocessor.CurrentMacros.Union( !InBuildPlayer ? new[] { "UNITY_EDITOR", } : new string[ 0 ] )
																		.Union( DefaultDefines )
																		.Where( macro => !string.IsNullOrEmpty( macro ) )
																		.Where( macro => !InBuildPlayer || !macro.StartsWith( "UNITY_EDITOR" ) ) );
			hash = MD5Helper.ComputeHash( defines.OrderBy( name => name.ToString() ).StringJoin( "," ) );
		}

#if !EJOY_BUILD_ASSETBUNDLE_APP
		[PreBuildPlayer]
#endif
		static Action OnPreBuildPlayer( ref string[] scenes, string path, BuildTarget target, ref BuildOptions options )
		{
			var			holder = new EnterInBuildPlayer();

			AssetDatabase.SaveAssets();

			SyncAllImpl( true, true );
			AssetDatabase.Refresh( ImportAssetOptions.DontDownloadFromCacheServer | ImportAssetOptions.ForceSynchronousImport );

			return () =>
			{
				holder.Release();
				holder = null;

				AssetDatabase.SaveAssets();
				SyncAllImpl( true, true );
				AssetDatabase.Refresh( ImportAssetOptions.DontDownloadFromCacheServer | ImportAssetOptions.ForceSynchronousImport );
			};
		}

#if !EJOY_BUILD_ASSETBUNDLE_APP
		[InitializeOnLoadMethod]
#endif
		static void AutoSyncAll()
		{
			if( !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode )
				SyncAllImpl( false, true, ignoreLuaCheck: true );
		}

		[MenuItem( "Generator/Lua Script/Sync All" )]
		public static void SyncAll()
		{
			AssetDatabase.SaveAssets();
			SyncAllImpl( true, ignoreLuaCheck: true );
			AssetDatabase.Refresh( ImportAssetOptions.DontDownloadFromCacheServer | ImportAssetOptions.ForceSynchronousImport );
		}

		[MenuItem( "Generator/Lua Script/Sync All with Check" )]
		public static void SyncAllWithCheck()
		{
            Debug.LogError("开始检查了");
			AssetDatabase.SaveAssets();
			SyncAllImpl( true,ignoreBlame:false);
			AssetDatabase.Refresh( ImportAssetOptions.DontDownloadFromCacheServer | ImportAssetOptions.ForceSynchronousImport );
		}

		static void SyncAllImpl( bool forceSync = false, bool silent = false, bool ignoreLuaCheck = false,bool ignoreBlame=true )
		{
#if EJOY_BUILD_ASSETBUNDLE_APP
			return;
#endif
			using( var db = new SerializedFile<Dictionary<string, string>>( DatabasePath ) )
			{
				//Debug.Log( "delete unused preprocessed lua script: " + TargetPath );
				if( Directory.Exists( TargetPath ) )
				{
					var			count = 0;

					foreach( var file in Directory.GetFiles( TargetPath ) )
						if( !file.StartsWith( DatabasePath ) && file.EndsWith( LuaResSuffix ) && !File.Exists( Res2Source( file ) ) )
						{
							DeleteFileWithMeta( file );
							++count;
                        }
					foreach( var directory in Directory.GetDirectories( TargetPath, "*", SearchOption.AllDirectories ).OrderByDescending( path => path.Length ) )
						if( Directory.GetFiles( directory ).Length == 0 && Directory.GetDirectories( directory ).Length == 0 )
							DeleteDirectoryWithMeta( directory, false );

					if( count > 0 )
						Debug.Log( "delete unused lua bytes: " + count + " @ " + TargetPath );
				}

				//Debug.Log( "sync all lua script: " + SourcePath );
				Assert.IsTrue( Directory.Exists( SourcePath ), SourcePath );

				HashSet<string>	defines;
				string			hash;
				BuildTarget		target;

				BuildScriptingDefines( out defines , out hash, out target );

				var			msg = "sync all: " + InBuildPlayer + " / " + defines.StringJoin( ", " ) + " @ " + target;

				if( !InBuildPlayer )
					Debug.Log( msg );
				else
					Debug.LogWarning( msg );

				var			files = Directory.GetFiles( SourcePath, "*" + LuaSuffix, SearchOption.AllDirectories ).Select( path => path.Replace( "\\", "/" ) ).ToArray();

				try
				{
					for( var i = 0; i < files.Length; ++i )
					{
						if( !silent )
							EditorUtility.DisplayProgressBar( "同步所有 lua 脚本", files[ i ], i / (float)files.Length );
						Sync( files[ i ], defines, hash, forceSync, ignoreLuacheck: ignoreLuaCheck,ignoreBlame:ignoreBlame);
					}
				}
				finally
				{
					if( !silent )
						EditorUtility.ClearProgressBar();
				}

				foreach( var path in db.Value.Keys.Where( path => !path.StartsWith( SourcePath ) || !File.Exists( path ) ).ToArray() )
					db.Value.Remove( path );
			}
		}

		static string Source2Res( string path )
		{
			return TargetPath + PathRegex.Match( path ).Groups[ 1 ].Value.Substring( SourcePath.Length ) + LuaResSuffix;
		}

		static string Res2Source( string path )
		{
			return SourcePath + PathRegex.Match( path ).Groups[ 1 ].Value.Substring( TargetPath.Length ) + LuaSuffix;
		}

		static string Source2ShortPath( string path )
		{
			return PathRegex.Match( path ).Groups[ 1 ].Value.Substring( SourcePath.Length ) + LuaResSuffix;
		}


        static bool Sync( string path, HashSet<string> macros, string macrosHash, bool forceSync = false, bool ignoreLuacheck = false,bool ignoreBlame = true )
		{
#if EJOY_BUILD_ASSETBUNDLE_APP
			return false;
#endif
			if( !path.StartsWith( SourcePath, true, null ) || !File.Exists( path ) || !path.EndsWith( LuaSuffix, true, null ) )
				return false;

#if LOG_DEBUG
			Debug.Log( "sync: " + path );
#endif
			using( var db = new SerializedFile<Dictionary<string, string>>( DatabasePath ) )
			{
				try
				{
					var			target = Source2Res( path );
					var			directory = Path.GetDirectoryName( target );
					var			changed = false;

					if( !Directory.Exists( directory ) )
					{
						Directory.CreateDirectory( directory );
						changed = true;
					}

					var			hash = string.Format( "{0}, {1}", MD5Helper.ComputeFileHash( path ), macrosHash );
					string		lastHash;

					if( forceSync || changed || !db.Value.TryGetValue( path, out lastHash ) || lastHash != hash )
					{
						try
						{
							var			original = File.ReadAllText( path );
							var			match = LuaCodeHelper.FirstSpaceLine.Match( original );

							//if( !match.Groups[ "SpaceLine" ].Success )
							//	Debug.LogError( "出于未来预处理的目的，lua文件第一行必须留空，多谢：" + path, AssetDatabase.LoadMainAssetAtPath( path ) );
							LuaGlobal2Local.Check( original, path );

							var			processed = LuaPreprocessor.Preprocess( original, macros, path );

							if( !InBuildPlayer )
							{ 
								File.WriteAllText( target, processed );

								if( !ignoreLuacheck )
								{
                                    if(!ignoreBlame)
                                    {
                                        if (string.IsNullOrEmpty(cmdProcess.StartInfo.FileName))
                                        {
                                            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                                            startInfo.FileName = "cmd.exe";
                                            startInfo.UseShellExecute = false;
                                            startInfo.RedirectStandardInput = false;
                                            startInfo.RedirectStandardOutput = true;
                                            startInfo.CreateNoWindow = true;
                                            cmdProcess.StartInfo = startInfo;
                                        }
                                        cmdProcess.StartInfo.Arguments = string.Format("/C" + @"svn blame {0}", path);
                                        LineToAnchor.Clear();
                                        if (cmdProcess.Start())
                                        {
                                            int line = 1;
                                            string lineStr = cmdProcess.StandardOutput.ReadLine();

                                            while (!string.IsNullOrEmpty(lineStr))
                                            {
                                                LineToAnchor[line.ToString()] = AuthorRegex.Match(lineStr).Groups[1].Value;
                                                line++;
                                                lineStr = cmdProcess.StandardOutput.ReadLine();
                                            }

                                        }
                                    }
                                    
                                    foreach ( var info in luacheck.execute( target ) )
									{
                                        string msg = null;
                                        if(!ignoreBlame)
                                        {
                                            var line = info.Position.Trim().Split(',')[0];
                                            var author = LineToAnchor[line];
                                            msg = string.Format("{0}({1}):{2}:\n{3}",  path, info.Position, author, info.Info);
                                        }
                                        else
                                        {
                                            msg = string.Format("{0}({1}):\n{2}", path, info.Position, info.Info);
                                        }
                                         if (info.IsError)
                                        {
                                            Debug.LogError(msg);
                                        }
                                        else
											Debug.LogWarning(msg );
									}
								}
							}
							else
							{
								var			shortPath = Source2ShortPath( path );

								Compiler( target, shortPath, processed );
							}

							db.Value[ path ] = hash;
						}
						catch( Exception ex )
						{
							Debug.LogError( path );
							if( !InBuildPlayer )
								Debug.LogException( ex );
							else
								throw ex;

							return false;
						}

						return true;
					}
				}
				catch( Exception ex )
				{
					if( !InBuildPlayer )
						Debug.LogException( ex );
					else
						throw ex;
				}

				return false;
			}
		}

		static bool Deleted( string path )
		{
			if( !path.StartsWith( SourcePath, true, null ) )
				return false;
			else if( Directory.Exists( path ) )
				return DirectoryDeleted( path );
			else if( path.EndsWith( LuaSuffix, true, null ) )
				return FileDeleted( path );
			else
				return false;
		}

		static bool DirectoryDeleted( string path )
		{
#if EJOY_BUILD_ASSETBUNDLE_APP
			return false;
#endif
#if LOG_DEBUG
			Debug.Log( "delete directory: " + path );
#endif
			using( var db = new SerializedFile<Dictionary<string, string>>( DatabasePath ) )
			{
				var			source = SourcePath + "/";

                foreach( var deleted in db.Value.Keys.Where( deleted => deleted.StartsWith( source ) ).ToArray() )
					db.Value.Remove( deleted );

				var			target = TargetPath + path.Substring( SourcePath.Length );

				if( Directory.Exists( target ) )
				{
					DeleteDirectoryWithMeta( target, true );
					return true;
				}
				else
					return false;
			}
		}

		static bool FileDeleted( string path )
		{
#if EJOY_BUILD_ASSETBUNDLE_APP
			return false;
#endif
#if LOG_DEBUG
			Debug.Log( "delete file: " + path );
#endif
			using( var db = new SerializedFile<Dictionary<string, string>>( DatabasePath ) )
			{
				db.Value.Remove( path );

				var			target = Source2Res( path );

				if( File.Exists( target ) )
				{
					DeleteFileWithMeta( target );
					return true;
				}
				else
					return false;
			}
		}

		static void DeleteFileWithMeta( string path )
		{
			File.Delete( path );

			var			meta = path + ".meta";

			if( File.Exists( meta ) )
				File.Delete( meta );
		}

		static void DeleteDirectoryWithMeta( string path, bool recursive )
		{
			Directory.Delete( path, recursive );

			var			meta = path + ".meta";

			if( File.Exists( meta ) )
				File.Delete( meta );
		}

		static void Compiler( string path, string shortPath, string sourceCode )
		{
			var			tempPath = "Temp/" + shortPath;
			var			dir = Path.GetDirectoryName( tempPath );

			if( !string.IsNullOrEmpty( dir ) && !Directory.Exists( dir ) )
				Directory.CreateDirectory( dir );

			var			luaPath = Path.ChangeExtension( tempPath, LuaSuffix );

			try
			{
				File.WriteAllText( luaPath, sourceCode );
				LuaHelper.Compile( luaPath, tempPath );
				File.Copy( tempPath, path, true );
			}
			finally
			{
				File.Delete( luaPath );
				File.Delete( tempPath );
			}
		}
	}
}
*/