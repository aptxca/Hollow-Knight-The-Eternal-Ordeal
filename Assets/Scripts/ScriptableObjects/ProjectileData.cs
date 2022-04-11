using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Demo2D
{
    [CreateAssetMenu(fileName = "ProjectileData", menuName = "Projectile Data", order = 100)]
    public class ProjectileData : ScriptableObject
    {
        public int damageDeal;
        public float speed;
        public MoveAxis forwardAxis = MoveAxis.YAxis;
        public bool ignoreGravity;
        public float gravity;
        public bool navigable;
        public float rotationAdjustSpeed;
        public float maxLifeTime;
        public bool destroyOnHit;
        public bool stopOnHit;
        public LayerMask targetMask;
        public LayerMask interactiveMask;
    }
}