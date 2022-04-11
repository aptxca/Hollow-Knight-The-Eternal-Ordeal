using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class ABEditor
{
    public static string editorAssetBundleConfigPath = "Assets/Editor/Resource/AssetBundleConfig.asset";

    //收录全部文件夹AB包
    protected static Dictionary<string, string> _dicDirABs = new Dictionary<string, string>();

    //收录全部的AB包所包含的全部路径,对于文件夹AB包，只收录文件夹目录，下面的文件目录不管; 对于文件AB包，收录文件路径,并且收录其依赖项的路径
    protected static HashSet<string> _allCollectedPath = new HashSet<string>();

    //收录全部文件AB包,这里的文件均为prefab
    protected static Dictionary<string, List<string>> _dicFileABs = new Dictionary<string, List<string>>();

    [MenuItem("Tools/打包")]
    public static void BuildAssetBundle()
    {
        _dicDirABs.Clear();
        _allCollectedPath.Clear();
        _dicFileABs.Clear();
        ParseABConfig(editorAssetBundleConfigPath);
        foreach (var name in _dicDirABs.Keys)
        {
            Debug.Log("文件夹AB包：" + name + " 路径： " + _dicDirABs[name]);
        }
        foreach (var name in _dicFileABs.Keys)
        {
            foreach (var path in _dicFileABs[name])
            {
                Debug.Log("AB包："+name+ " 包含："+path);
            }   
        }
        foreach (var path in _allCollectedPath)
        {
            Debug.Log("全部收录的路径：" + path);
        }


    }


    private static void ParseABConfig(string path)
    {
        ABConfig config = AssetDatabase.LoadAssetAtPath<ABConfig>(path);
        if (config == null)
        {
            Debug.LogError("编辑器找不到AB包配置文件：" + path);
            return;
        }
        ParseABConfig(config);
    }
    private static void ParseABConfig(ABConfig config)
    {
        foreach (var info in config.dirAB)
        {
            _dicDirABs.Add(info.ABName,info.Path);
            _allCollectedPath.Add(info.Path);
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", config.path.ToArray());

        foreach (var guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            string[] dependencies = AssetDatabase.GetDependencies(path);
            List<string> allDependPath = new List<string>();//临时收录文件依赖项的列表
            foreach (var dependPath in dependencies)
            {
                if (AlreayCollected(dependPath) || dependPath.EndsWith(".cs"))//依赖项已经收录到AB包里了，或者依赖项为脚本
                {
                    continue;
                }

                if (dependPath == path) //依赖项路径等于自身的路径，因为依赖项数组中必然存有自身路径
                {
                    allDependPath.Add(dependPath);
                    _allCollectedPath.Add(dependPath);
                }

            }
            _dicFileABs.Add(obj.name, allDependPath);

        }

    }

    private static bool AlreayCollected(string newPath)
    {
        foreach (var path in _allCollectedPath)
        {
            if (newPath == path || (newPath.Contains(path)&& newPath.Replace(path,"").StartsWith("/")))//有两种情况：1、作为依赖项时被收录 2、作为文件夹AB包下的文件被收录 
            {
                return true;
            }
        }

        return false;
    }
}
