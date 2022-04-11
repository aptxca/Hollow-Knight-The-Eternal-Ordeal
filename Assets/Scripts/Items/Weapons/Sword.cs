using System;
using System.Collections.Generic;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    public class Sword : MonoBehaviour
    {
        public int baseDamage;
        public int baseKnockBack;
        public LayerMask targetLayer;
        public MonoAudioModule auduoModule;
        public GameObject Owner { get; protected set; }
        public bool HitSomething { get; set; }
        public Vector2 HitFeedback { get; protected set; }

        private Animator _animator;


        private int _hashAttackDirection = Animator.StringToHash("AttackDirection");
        private int _hashSkillIndex = Animator.StringToHash("SkillIndex");
        private int _hashChargeTime = Animator.StringToHash("ChargeTime");
        private int _hashAnimStateAttackFwd = Animator.StringToHash("Attack_Fwd");
        private int _hashAnimStateAttackUp = Animator.StringToHash("Attack_Up");
        private int _hashAnimStateAttackDown = Animator.StringToHash("Attack_Down");
        private int _hashAnimStateSwordArt1 = Animator.StringToHash("SwordArt 1");
        private int _hashAnimStateSwordArt2 = Animator.StringToHash("SwordArt 2");
        private int _hashAnimStateSwordArt3 = Animator.StringToHash("SwordArt 3");
        private int _hashAnimStateCharge = Animator.StringToHash("Charge");

        private Vector2 _hitDirection;

        public void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void OnEnable()
        {
            HitSomething = false;
        }

        public void Attack(float direction, int skillIndex)
        {
            _animator.SetFloat(_hashAttackDirection, direction);
            _animator.SetInteger(_hashSkillIndex, skillIndex);
            int animStateHash = 0;
            switch (skillIndex)
            {
                case 0:
                    if (direction > 0.6f)
                    {
                        animStateHash = _hashAnimStateAttackUp;
                        _hitDirection = Vector2.up;
                    }
                    else if (direction < -0.6f)
                    {
                        animStateHash = _hashAnimStateAttackDown;
                        _hitDirection = Vector2.down;
                    }
                    else
                    {
                        animStateHash = _hashAnimStateAttackFwd;
                        _hitDirection = -transform.right;
                    }

                    auduoModule.PlayAudioRandom(AudioType.Attack);
                    break;
                case 1:
                    animStateHash = _hashAnimStateSwordArt1;
                    auduoModule.PlayAudio(AudioType.SwordArt,0);
                    break;
                case 2:
                    animStateHash = _hashAnimStateSwordArt2;
                    auduoModule.PlayAudio(AudioType.SwordArt, 1);

                    break;
                case 3:
                    animStateHash = _hashAnimStateSwordArt3;
                    auduoModule.PlayAudio(AudioType.SwordArt, 0);
                    break;

            }

            _animator.Play(animStateHash, -1, 0f);
        }

        public void Charge(float time)
        {
            _animator.Play(_hashAnimStateCharge, -1, 0f);
        }

        public  void Attach(Transform owner)
        {
            Owner = owner.gameObject;
            //transform.SetParent(owner, false);
        }

        public void OnTriggerEnter2D(Collider2D collider)
        {
            CheckHit(collider);
        }


        protected bool CheckHit(Collider2D collider)
        {
            if (collider.gameObject == Owner)
            {
                return true;
            }

            //打到非目标layer,返回
            if (!targetLayer.ContainLayer(collider.gameObject.layer))
            {
                return false;
            }


            switch (collider.tag)
            {
                case "Enemy":
                    DamageData msg = new DamageData(this, Owner, _hitDirection, baseDamage, baseKnockBack);
                    Damageable damageable = collider.gameObject.GetComponent<Damageable>();
                    if (damageable ==null)
                    {
                        damageable = collider.gameObject.GetComponentInParent<Damageable>();
                    }
                    if (damageable!=null)
                    {
                        damageable.ApplyDamage(msg);
                    }
                    HitSomething = true;
                    HitFeedback = -_hitDirection;
                    break;
                case "Platform":
                    break;
                case "Water":
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}