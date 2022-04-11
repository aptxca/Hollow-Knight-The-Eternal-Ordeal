using System;
using System.Collections.Generic;
using Demo2D.Frame;
using UnityEngine;

namespace Demo2D
{
    public class DamageData
    {
        //近战武器、投射物等直接作用到被攻击者的物体
        public MonoBehaviour damager;
        //武器的持有者，投射物的持有者
        public GameObject damageSource;
        //伤害方向
        public Vector2 direction;
        //伤害
        public int damage;
        //击退
        public int knockBack;

        public DamageData(MonoBehaviour damager, GameObject source, Vector2 direction, int damage, int knockBack)
        {
            Set(damager,source,direction,damage,knockBack);
        }

        public void Set(MonoBehaviour damager, GameObject source, Vector2 direction, int damage, int knockBack)
        {
            this.damager = damager;
            this.damageSource = source;
            this.direction = direction;
            this.damage = damage;
            this.knockBack = knockBack;
        }

        public void Reset()
        {
            Set(null, null, Vector2.zero, 0, 0);
        }
    }

    public class DamageTrigger : ITrigger
    {
        public DamageData Damage { get; set; }
        public bool IsDead { get; set; }

        private Action<ITrigger> _onTriggered;
        public void AddListener(Action<ITrigger> listener)
        {
            _onTriggered += listener;
        }

        public void RemoveListener(Action<ITrigger> listener)
        {
            _onTriggered -= listener;
        }

        public void Trigger()
        {
            _onTriggered?.Invoke(this);
        }
    }


    [Serializable]
    public class Damageable : MonoBehaviour
    {
        public CharacterStateInfo currentStateInfo = new CharacterStateInfo();
        public CharacterStateInfo maxStateInfo = new CharacterStateInfo();
        public DamageTrigger damageTrigger = new DamageTrigger();

        public bool IsInvulnerable
        {
            get => currentStateInfo.invulnerable;
            set => currentStateInfo.invulnerable = value;
        }


        public void ApplyDamage(DamageData data)
        {
            if (currentStateInfo.hp.Data<=0)
            {
                return;
            }

            if (currentStateInfo.invulnerable)
            {
                return;
            }

            int hp = currentStateInfo.hp.Data - data.damage;

            if (hp <= 0)
            {
                currentStateInfo.hp.Data = 0;
                damageTrigger.IsDead = true;
            }
            else
            {
                currentStateInfo.hp.Data = hp;
                damageTrigger.IsDead = false;
            }

            damageTrigger.Damage = data;
            damageTrigger.Trigger();
            if (!damageTrigger.IsDead)
            {
                currentStateInfo.invulnerable = true;
            }
        }


        public void ResetDamage()
        {
            currentStateInfo.hp.Data = maxStateInfo.hp.Data;
            currentStateInfo.mp.Data = maxStateInfo.mp.Data;
            currentStateInfo.invulnerable = false;
        }


        void Start()
        {
            ResetDamage();
        }

        void Update()
        {
            if (currentStateInfo.invulnerable)
            {
                currentStateInfo.invulnerableTime += Time.deltaTime;
                if (currentStateInfo.invulnerableTime >= maxStateInfo.invulnerableTime)
                {
                    currentStateInfo.invulnerable = false;
                    currentStateInfo.invulnerableTime = 0;
                }
            }

        }

        public void SetInvulnerable(float duration)
        {

        }

        public void Heal(int count)
        {
            int hp = count + currentStateInfo.hp.Data;
            currentStateInfo.hp.Data = hp>maxStateInfo.hp.Data ? maxStateInfo.hp.Data : hp;
        }

    }
}