using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Demo2D.Utility;


namespace Demo2D
{
    [Serializable]
    public class GameObjectResource
    {
        #region resource资源相关
        public string path;
        [HideInInspector] public uint crc;
        [HideInInspector] public Hash128 hash;
        public ResourceItem resItem;
        #endregion

        #region GameObject对象相关

        //实例化对象的guid
        public int guid;
        //实例化的对象是否正在被使用
        public bool inUse;
        //public ReactiveProperty<bool> inUse;
        //实例化的对象
        public GameObject objClone;

        #endregion
    }


    public class GameObjectManager : Singleton<GameObjectManager>
    {
        private Dictionary<Hash128, List<GameObjectResource>> _gameObjectPoolDic = new Dictionary<Hash128, List<GameObjectResource>>();

        private Dictionary<int, GameObjectResource> _gameObjectDic = new Dictionary<int, GameObjectResource>();

        private List<int> _gameObjectClearList = new List<int>();
        private List<Hash128> _gameObjectPoolClearList = new List<Hash128>();
        public Transform createTrans;

        public GameObjectResource InstantiateGameObjectRes(string path, bool setParent = true, bool createNew = true)
        {
            Hash128 hash = Hash128.Compute(path);
            uint crc = Crc32.GetCrc32(path);
            GameObjectResource objRes = null;

            if (createTrans == null)
            {
                GameObject create = new GameObject("_CreatedByGameObjectManager");
                createTrans = create.transform;
            }

            //尝试从缓存池中查找
            if (_gameObjectPoolDic.TryGetValue(hash, out var objList)&&objList!=null&&objList.Count>0)
            {
                objRes = objList[0];
                _gameObjectDic.Add(objRes.guid, objRes);
                objList.RemoveAt(0);
                if (objRes.objClone !=null)
                {
                    return objRes;
                }
                //Debug.Log("缓存池中gameobject为空，可能被系统回收");
            }

            //如果不允许从资源池中创建，就此返回
            if (!createNew)
            {
                return objRes;
            }

            //尝试从资源池中实例化一个对象出来
            objRes ??= new GameObjectResource();
            objRes.path = path;
            objRes.hash = hash;
            objRes.crc = crc;
            objRes = GetGameObjectResFromResMgr(objRes);

            //资源加载成功,在对象池开辟空间，以便后续对象回收使用
            if (objRes.objClone!=null)
            {
                if (setParent)
                {
                    objRes.objClone.transform.SetParent(createTrans,false);
                }
                objList ??= new List<GameObjectResource>();
                if (!_gameObjectPoolDic.ContainsKey(hash))
                {
                    _gameObjectPoolDic.Add(hash,objList);
                }
            }
            if (objRes.objClone == null)
            {
                Debug.Log("error1");
            }
            return objRes;
        }

        private GameObjectResource GetGameObjectResFromResMgr(GameObjectResource objRes)
        {
            if (ResourceManager.Instance.LoadGameObjectRes(ref objRes))
            { 
                objRes.objClone = GameObject.Instantiate(objRes.resItem.resource as GameObject);
                objRes.guid = objRes.objClone.GetInstanceID();
                _gameObjectDic.Add(objRes.guid, objRes);
            }
            else
            {
                Debug.Log("GameObject资源加载失败");
            }
            return objRes;
        }

        public GameObject InstantiateGameObject(string path , bool setParent =true, bool createNew = true)
        {
            GameObjectResource objRes = InstantiateGameObjectRes(path, setParent, createNew);

            if (objRes != null && objRes.objClone != null)
            {
                objRes.inUse = true;
                objRes.objClone.SetActive(true);
                return objRes.objClone;
            }
            return null;
        }

        public void Recycle(GameObject obj, bool resetParent =false)
        {
            int guid = obj.GetInstanceID();
            if (!_gameObjectDic.TryGetValue(guid,out var gameObjectRes))
            {
                Debug.Log("该资源: "+obj.name+ " 未被GameObjectManager收录,不是由GameObjectManager创建的");
                return;
            }

            if (!_gameObjectPoolDic.TryGetValue(gameObjectRes.hash,out var objList))
            {
                Debug.LogError("GameObject对象池中找不到对应的列表");
                return;
            }

            if (objList==null)
            {
                Debug.Log("对象列表为空");
                return;
            }
            obj.SetActive(false);
            if (resetParent)
            {
                obj.transform.SetParent(createTrans,false);
            }
            objList.Add(gameObjectRes);
            _gameObjectDic.Remove(guid);
            gameObjectRes.inUse = false;
        }

        public int GetObjectCacheCount(string path,out List<GameObjectResource> resList)
        {
            
            Hash128 hash = Hash128.Compute(path);
            if (_gameObjectPoolDic.TryGetValue(hash,out var objResList)&&objResList!=null)
            {
                resList = objResList;
                return objResList.Count;
            }
            resList = objResList;
            return 0;
        }

        public  IEnumerator ClearCacheAsync(Action callback=null)
        {
            _gameObjectClearList.Clear();
            _gameObjectPoolClearList.Clear();
            foreach (var guid in _gameObjectDic.Keys)
            {
                if (_gameObjectDic[guid].objClone == null)
                {
                    _gameObjectClearList.Add(guid);
                }
            }

            foreach (var guid in _gameObjectClearList)
            {
                _gameObjectDic.Remove(guid);
            }

            foreach (var hash in _gameObjectPoolDic.Keys)
            {
                _gameObjectPoolDic[hash].RemoveAll(objRes => objRes.objClone == null);
            }

            foreach (var hash in _gameObjectPoolDic.Keys)
            {
                if (_gameObjectPoolDic[hash].Count == 0 &&
                    !_gameObjectDic.Values.Select(objRes => objRes.hash).Contains(hash))
                {
                    _gameObjectPoolClearList.Add(hash);
                }
            }

            foreach (var hash in _gameObjectPoolClearList)
            {
                _gameObjectPoolDic.Remove(hash);
            }

            yield return null;
        }

    }

}