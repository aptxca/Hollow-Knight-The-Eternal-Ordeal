using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Demo2D
{
    public class JumperZote : Zote
    {
        public float jumpSpeed;
        public float maxHorizentalSpeed;

        public float idleTime = 3f;


        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.Jump))
                .AddState(new ZoteFsmState(ZoteState.Falling))
                .AddState(new ZoteFsmState(ZoteState.Landing))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));
            var allState = characterFsmSystem.States;


            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter)
                .AddTransition(new ZoteTransition(() => !IsGrounded), allState[ZoteState.Falling])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > idleTime&& IsGrounded), allState[ZoteState.Jump])
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit]);



            allState[ZoteState.Jump].RegisterProcess(OnJumpEnter,null, OnJumpUpdate)
                .AddTransition(new ZoteTransition(() => VerticalSpeed<0f), allState[ZoteState.Falling])
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit]);

            allState[ZoteState.Falling].RegisterProcess(OnFallingEnter,null,OnFallingUpdate)
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit])
                .AddTransition(new ZoteTransition(() => IsGrounded), allState[ZoteState.Landing]);

            allState[ZoteState.Landing].RegisterProcess(OnLandEnter)
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit])
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.Idle]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.None]);


            allState[ZoteState.Hit].RegisterProcess(OnHitEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > 0.2f && IsGrounded), allState[ZoteState.Idle])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > 0.2f && !IsGrounded), allState[ZoteState.Falling]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);


            characterFsmSystem.SetDefaultState(ZoteState.Idle);

        }


        #region 状态机回调


        protected override void OnJumpEnter(FsmState prev, FsmTransition trans)
        {
            if (IsTargetAtLeft)
            {
                ChangeHeading(true);
                HorizontalSpeed = Random.Range(-maxHorizentalSpeed, -maxHorizentalSpeed * 0.4f);
            }
            else
            {
                ChangeHeading(false);

                HorizontalSpeed = Random.Range(maxHorizentalSpeed * 0.4f, maxHorizentalSpeed);
            }
            VerticalSpeed = Random.Range(0.5f * jumpSpeed, jumpSpeed);
            base.OnJumpEnter(prev, trans);
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