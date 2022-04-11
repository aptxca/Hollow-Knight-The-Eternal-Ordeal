using System;
using System.Collections;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    public enum MoveAxis
    {
        XAxis,
        YAxis,
        ZAxis,
    }

    [Serializable]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile:MonoBehaviour
    {
        public ProjectileData data;
        public Vector2 direction;
        public float lifeTime;

        public Shooter owner;
        public GameObject target;

        public MonoEffectModule effectModule;
        public MonoAudioModule audioModule;
        public Animator animator;

        private Collider2D _collider;

        private IEnumerator _moveCoroutine;

#if UNITY_EDITOR
        private Ray _ray;
#endif

        public void Init(ProjectileData data, Shooter owner, GameObject target, ShootPoint point)
        {
            this.data = data;
            this.owner = owner;
            this.target = target;
            //gameObject.layer = LayerMask.NameToLayer("Enemy");
            transform.position = point.transform.position;
            if (target!=null)
            {
                direction = (target.transform.position - transform.position).normalized;
            }
            else
            {
                direction = point.direction;
            }

        }

        public void Shoot()
        {
            animator.Play("Shoot");
            _moveCoroutine = MoveCoroutine();
            StartCoroutine(_moveCoroutine);
        }

        public void OnTriggerEnter2D(Collider2D collider)
        {
            int colLayer = collider.gameObject.layer;

            //击中需要交互的层级
            if (data.interactiveMask.ContainLayer(colLayer))
            {

                animator.Play("Hit");
                //audioModule.PlayAudioRandom(AudioType.Hit);
                //effectModule.Play(EffectType.Explode, 0);
                if (data.stopOnHit)
                {
                    StopCoroutine(_moveCoroutine);
                }
                if (data.destroyOnHit)
                {
                    StartCoroutine(ReleaseWait());
                }
            }

            //击中目标层级
            if (collider.gameObject == target||data.targetMask.ContainLayer(colLayer))
            {
                DamageData data = new DamageData(this, owner.gameObject, collider.transform.position - transform.position,
                    this.data.damageDeal, 1);
                Damageable damageable = collider.gameObject.GetComponent<Damageable>();
                if (damageable !=null)
                {
                    damageable.ApplyDamage(data);
                }
            }

        }




        public void Reset()
        {
            transform.eulerAngles = Vector3.zero;
        }

        public void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void OnDisable()
        {
            Reset();
        }

        private IEnumerator MoveCoroutine()
        {
            Vector2 currentSpeed = data.speed * direction;
            lifeTime = 0f;
            while (lifeTime<data.maxLifeTime)
            {
                if (!data.ignoreGravity)
                {
                    currentSpeed += data.gravity * Time.deltaTime* Vector2.down;
                }

                if (data.navigable)
                {
                    Vector2 delta = target.transform.position - transform.position;
                    Quaternion rot = Quaternion.FromToRotation(currentSpeed,delta);
                    currentSpeed = Quaternion.RotateTowards(Quaternion.Euler(currentSpeed), rot, data.rotationAdjustSpeed*Time.deltaTime) *
                                   currentSpeed;
                }
#if UNITY_EDITOR
                _ray.origin = transform.position;
                _ray.direction = currentSpeed;
#endif

                switch (data.forwardAxis)
                {
                    case MoveAxis.YAxis:
                        transform.rotation = Quaternion.FromToRotation(Vector3.up, currentSpeed);
                        break;
                    case MoveAxis.XAxis:
                        transform.rotation = Quaternion.FromToRotation(Vector3.right, currentSpeed);
                        break;
                    case MoveAxis.ZAxis:
                        transform.rotation = Quaternion.FromToRotation(Vector3.forward, currentSpeed);
                        break;
                }

                transform.Translate(currentSpeed * Time.deltaTime, Space.World);
                lifeTime += Time.deltaTime;
                yield return null;
            }

            yield return StartCoroutine(ReleaseWait());
        }

        private IEnumerator ReleaseWait()
        {
            if (animator!=null)
            {
                yield return null;
                while (!animator.IsCurrentStateEnd(0))
                {
                    yield return null;
                }
            }

            if (audioModule != null)
            {
                while (audioModule.IsPlaying())
                {
                    yield return null;
                }
            }

            if (effectModule != null)
            {
                while (effectModule.IsPlaying())
                {
                    yield return null;
                }
            }

            GameObjectManager.Instance.Recycle(gameObject);

        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.DrawRay(_ray);
        }
#endif
    }
}