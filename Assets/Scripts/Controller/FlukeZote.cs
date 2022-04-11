using System.Collections;
using Cinemachine;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{

    public class FlukeZote : Zote
    {
        public float raiseSpeed;
        public Shooter shooter;
        public float shootInterval;

        private float _shootTime;
        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem.AddState(new ZoteFsmState(ZoteState.Entrance))
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));
            var allState = characterFsmSystem.States;

            allState[ZoteState.Entrance].RegisterProcess(OnEntranceEnter)
                .AddTransition(new ZoteTransition(()=>animator.IsCurrentStateEnd(0)), allState[ZoteState.Idle]);

            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter, null, OnIdleUpdate)
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit])
                .AddTransition(new ZoteTransition(IsOutOfCamera), allState[ZoteState.None]);

            allState[ZoteState.Hit].RegisterProcess(OnHitEnter)
                .AddTransition(new ZoteTransition(() => !IsDead && allState[ZoteState.Hit].Time > 0.2f), allState[ZoteState.Idle])
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.None]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            characterFsmSystem.SetDefaultState(ZoteState.Entrance);
        }

        public bool IsOutOfCamera()
        {
            Vector2 vpPis = Camera.main.WorldToViewportPoint(transform.position);
            return vpPis.y > 1.1f;
        }

        #region 状态机回调
        protected override void OnIdleUpdate()
        {
            VerticalSpeed = raiseSpeed;
            HorizontalSpeed = 0f;

            _shootTime += Time.deltaTime;
            if (_shootTime > shootInterval)
            {
                shooter.ShootProjectile(null, 0);
                _shootTime = 0f;
            }


        }

        protected override void OnHitEnter(FsmState prev, FsmTransition trans)
        {
            base.OnHitEnter(prev, trans);
            DamageTrigger trigger = trans.Trigger as DamageTrigger;
            DamageData damage = trigger.Damage;
            HorizontalSpeed = (damage.direction * damage.knockBack).x;
            VerticalSpeed = (damage.direction * damage.knockBack).y;
        }

        #endregion
    }
}