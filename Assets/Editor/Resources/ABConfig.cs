using System;
using System.Collections.Generic;
using UnityEngine;


//两种打包方式的AB包，1、将文件夹作为AB包，文件夹下面的文件都归属于该B包；2、以文件夹为路径，文件夹下面的每个prefab生成AB包

[CreateAssetMenu(fileName = "AssetBundleConfig", menuName = "ABConfig",order = 0)]
public class ABConfig : ScriptableObject
{
    //文件夹AB包
    public List<ABBase> dirAB;

    //文件AB包
    public List<String> path;
}

[Serializable]
public class ABBase
{
    public string ABName;
    public string Path;
}
