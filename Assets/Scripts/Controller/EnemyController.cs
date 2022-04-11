using System;
using System.Collections;
using System.Collections.Generic;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    public enum DirectionType
    {
        Up,
        Down,
        Left,
        Right
    }

    public class EnemyController : CharaterController2D
    {

        public bool rebounce;
        public LayerMask rebounceMask;
        protected TargetScanner scanner;
        protected GroundDetector groundDetector;

        void Awake()
        {
            Init();
        }


        public override void Init()
        {
            scanner = GetComponent<TargetScanner>();
            damageable = GetComponent<Damageable>();
            groundDetector = GetComponentInChildren<GroundDetector>();
            _rigidbody = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            animator.applyRootMotion = false;
        }


        public override bool IsGrounded => groundDetector!=null && groundDetector.IsGrounded;
        public bool IsHeadingLeft => transform.rotation.y == 0;
        public bool IsTargetAtLeft => scanner.FindTarget && (scanner.target.position.x < transform.position.x);

        public bool IsAllStopped()
        {
            if (!animator.IsCurrentStateEnd(0))
            {
                return false;
            }

            if (audioModule !=null&&audioModule.IsPlaying())
            {
                return false;
            }

            if (effectModule!=null&&effectModule.IsPlaying())
            {
                return false;
            }

            return true;
        }
        protected override Platform Plat { get; }

        public bool IsOutOfCamera(DirectionType dirType,float offSet =0.1f)
        {
            Vector2 vpPis = Camera.main.WorldToViewportPoint(transform.position);
            switch (dirType)
            {
                case DirectionType.Up:
                    return vpPis.y > 1f + offSet;
                case DirectionType.Down:
                    return vpPis.y < 0f - offSet;
                case DirectionType.Right:
                    return vpPis.x > 1f + offSet;
                case DirectionType.Left:
                    return vpPis.x < 0f - offSet;
                default: return false;
            }
        }

        protected virtual void ResetStatus()
        {
            damageable.ResetDamage();
        }



        protected void OnCollisionEnter2D(Collision2D collision)
        {
            if (rebounce && rebounceMask.ContainLayer(collision.gameObject.layer) && collision.contactCount > 0)
            {
                Vector2 normal = collision.GetContact(0).normal;
                Vector2 inSpeed, outSpeed;
                inSpeed.x = HorizontalSpeed;
                inSpeed.y = VerticalSpeed;
                //往上的法线不做反弹，有可能是地面
                if (normal.y < 0.01f)
                {
                    outSpeed = Vector2.Reflect(inSpeed, normal);
                    HorizontalSpeed = outSpeed.x * 0.6f;
                    VerticalSpeed = outSpeed.y * 0.6f;
                }
            }

            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                Damageable damageable = collision.gameObject.GetComponent<Damageable>();
                if (damageable!=null)
                {
                    damageable.ApplyDamage(new DamageData(this,gameObject, collision.gameObject.transform.position- transform.position,1,1 ));
                }
            }
        }


    }

    
}