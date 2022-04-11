using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    public class FlyZote : Zote
    {
        public float moveSpeed;


        public override void InitFsm()
        {
            characterFsmSystem = new ZoteFsmSystem();

            characterFsmSystem.AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));
            var allState = characterFsmSystem.States;

            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter, null, OnIdleUpdate)
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit]);

            allState[ZoteState.Hit].RegisterProcess(OnHitEnter)
                .AddTransition(new ZoteTransition(() => !IsDead && CurrentStateTime > 0.2f), allState[ZoteState.Idle])
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), allState[ZoteState.None]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            characterFsmSystem.SetDefaultState(ZoteState.Idle);
        }



        #region 状态机相关回调

        protected override void OnIdleEnter(FsmState prev, FsmTransition trans)
        {
            base.OnIdleEnter(prev, trans);
            audioModule.PlayAudioRandom(AudioType.Fly);
        }

        protected override void OnIdleUpdate()
        {
            if (scanner.FindTarget)
            {
                Vector2 direction = (scanner.target.position - transform.position).normalized;
                HorizontalSpeed = moveSpeed * direction.x;
                ChangeHeading(HorizontalSpeed < 0f);

                VerticalSpeed = moveSpeed * direction.y;
            }
            else
            {
                HorizontalSpeed = VerticalSpeed = 0f;
            }

        }

        protected override void OnHitEnter(FsmState prev, FsmTransition trans)
        {
            base.OnHitEnter(prev, trans);

            DamageTrigger trigger =  trans.Trigger as DamageTrigger;
            DamageData damage = trigger.Damage;
            HorizontalSpeed = (damage.direction * damage.knockBack).x;
            VerticalSpeed = (damage.direction * damage.knockBack).y;

        }

        protected override void OnDeadEnter(FsmState prev, FsmTransition trans)
        {
            base.OnDeadEnter(prev, trans);
            audioModule.StopPlay(AudioType.Fly);
        }

        #endregion
    }
}