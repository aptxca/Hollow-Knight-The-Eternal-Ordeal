using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Demo2D
{

    [Serializable]
    public class ShootPoint
    {
        public Transform transform;
        public Vector2 direction;

    }


    public class Shooter : MonoBehaviour
    {
        public List<ShootPoint> origins;
        public ProjectileData projectileData;
        public GameObjectResource projectileRes = new GameObjectResource();

        public Projectile InstantiateProjectile()
        {
            projectileRes.objClone  =  GameObjectManager.Instance.InstantiateGameObject(projectileRes.path);
            Projectile res = projectileRes.objClone.GetComponent<Projectile>();
            if (res ==null)
            {
                Debug.Log("获取的gameObject上没有Projectile脚本");
                GameObject.Destroy(res.gameObject);
            }
            return res;
        }

        public void ShootProjectile(GameObject target,int startPointIndex)
        {
            Projectile orange = InstantiateProjectile();
            orange.Init(projectileData,this,target,origins[startPointIndex]);
            orange.Shoot();
        }
    }
}