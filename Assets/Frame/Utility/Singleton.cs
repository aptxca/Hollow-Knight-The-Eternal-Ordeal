using System;
using System.Reflection;
using UnityEngine;


namespace Demo2D.Utility
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                Type type = typeof(T);
                if (_instance == null)
                {
                    _instance = new T();
                }

                return _instance;
            }
        }


    }




    public class MonoSingleton<T> : MonoBehaviour where T:MonoBehaviour
    {
        private static T _instance;

        public static T Instance{
            get{
                if (_instance ==null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance !=null)
                    {
                        return _instance;
                    }
                    GameObject obj = new GameObject();
                    obj.name = typeof(T).Name;
                    _instance = obj.AddComponent<T>();
                }
                return _instance;
            }
        }

        public virtual void  Awake()
        {
            CheckSingleton(true);
        }

        protected bool CheckSingleton(bool dontDestroy)
        {
            if (Instance != this)
            {
                Destroy(gameObject);
                return false;
            }

            if (dontDestroy)
            {
                DontDestroyOnLoad(gameObject);
            }

            return true;
        }

    }
}