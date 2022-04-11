using System;
using System.Collections.Generic;
using System.IO;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    [Serializable]
    public class ResourceItem
    {
        public string path;
        public uint crc;
        public Hash128 hash;
        public UnityEngine.Object resource;
        public int refCount;
    }


    public class ResourceManager:Singleton<ResourceManager>
    {
#if UNITY_EDITOR
        private bool editorLoad = false;
#endif

        private Dictionary<Hash128, ResourceItem> _resourceItemPoolDic = new Dictionary<Hash128, ResourceItem>();


        public T LoadResource<T>(uint crc) where T : UnityEngine.Object
        {
            //todo:从AB包中加载
            return null;
        }

        public T LoadResource<T>(Hash128 hash) where T : UnityEngine.Object
        {



            //todo:从AB包中加载
            return null;
        }

        public T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            uint crc = Crc32.GetCrc32(path);
            Hash128 hash = Hash128.Compute(path);

            T res = null;
            ResourceItem resItem = LoadResourceItem(path);

            if (resItem.resource!=null)
            {
                res = resItem.resource as T;
                resItem.refCount++;
            }

            return res;
        }


        public bool LoadGameObjectRes(ref GameObjectResource gameObjectRes)
        {
            string path = gameObjectRes.path;
            Hash128 hash = Hash128.Compute(path);
            uint crc = Crc32.GetCrc32(path);

            ResourceItem resItem = LoadResourceItem(path);
            gameObjectRes.resItem = resItem;
            return resItem!=null;
        }

        private ResourceItem LoadResourceItem(string path)
        {
            uint crc = Crc32.GetCrc32(path);
            Hash128 hash = Hash128.Compute(path);

            //先从资源池中找
            ResourceItem resItem = null;
            if (_resourceItemPoolDic.TryGetValue(hash, out resItem) && resItem != null)
            {
                if (resItem.resource != null)
                {
                    resItem.refCount++;
                    return resItem;
                }
                else
                {
                    Debug.Log("resourceItem中的资源已经被释放，重新加载资源");
                }
            }
            else
            {
                //Debug.Log("资源池中未找到缓存资源，从磁盘加载资源");
                _resourceItemPoolDic.Add(hash, new ResourceItem());
                resItem = _resourceItemPoolDic[hash];
            }


            //开始从磁盘文件加载资源
            resItem.path = path;
            resItem.hash = hash;
            resItem.crc = crc;
            resItem.resource = null;

#if UNITY_EDITOR
            if (editorLoad)
            {
                resItem.resource = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (resItem.resource == null)
                {
                    Debug.Log("加载资源失败：" + path);
                }
                return resItem;
            }
#endif
            //从Resources目录加载
            if (resItem.resource == null)
            {
                string resPath = path.Remove(path.LastIndexOf('.')).Substring(path.IndexOf("Resources/", StringComparison.Ordinal)+10);
                resItem.resource = Resources.Load(resPath);
                if (resItem.resource == null)
                {
                    Debug.Log("Resources加载资源失败：" + resPath);
                }
            }
            return resItem;
            //todo:从AB包中加载

        }
    }
}