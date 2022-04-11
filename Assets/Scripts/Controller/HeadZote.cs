using System.Collections;
using System.Collections.Generic;
using Demo2D;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    public class HeadZote : Zote
    {
        public float lowHpThrethold = 8;
        public float waiteDropTime;
        public float exitTime;
        public float exitSpeed;

        public Shooter shooter;


        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Entrance))
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Falling))
                .AddState(new ZoteFsmState(ZoteState.Landing))
                .AddState(new ZoteFsmState(ZoteState.Leave))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));
            var allState = characterFsmSystem.States;

            allState[ZoteState.Entrance].RegisterProcess(OnEntranceEnter)
                .AddTransition(new ZoteTransition(()=> allState[ZoteState.Entrance].Time>waiteDropTime), allState[ZoteState.Falling]);

            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(()=>CurrentStateTime>exitTime), allState[ZoteState.Leave]);

            allState[ZoteState.Falling].RegisterProcess(null,null, OnFallingUpdate)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => IsGrounded), allState[ZoteState.Landing]);

            allState[ZoteState.Landing].RegisterProcess(OnLandEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() =>CurrentStateTime > 0.2f), allState[ZoteState.Idle]);

            allState[ZoteState.Leave].RegisterProcess(null,null, OnLeaveUpdate)
                .AddTransition(new ZoteTransition(() => IsOutOfCamera(DirectionType.Up)), allState[ZoteState.None]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), allState[ZoteState.None]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            characterFsmSystem.RegisterAnyStateTrigger(damageable.damageTrigger,OnHitCallBack);

            characterFsmSystem.SetDefaultState(ZoteState.Entrance);
        }

        //全部状态的共同的受击回调函数
        private void OnHitCallBack(ITrigger trigger)
        {
            if (!IsDead)
            {
                audioModule.PlayAudioRandom(AudioType.Hit);
                hitEffect?.Play();
                if (damageable.currentStateInfo.hp.Data < lowHpThrethold)
                {
                    animator.Play("Low_Hp", 0, 0);
                }
                else
                {
                    animator.Play("Idle", 0, 0);
                }
            }
        }


        #region 状态机相关回调

        protected override void OnEntranceEnter(FsmState arg1, FsmTransition arg2)
        {
            VerticalSpeed = -0.2f;
        }

        protected override void OnLandEnter(FsmState prev, FsmTransition trans)
        {
            base.OnLandEnter(prev, trans);
            shooter.ShootProjectile(null,0);
            shooter.ShootProjectile(null,1);
        }

        protected override void OnLeaveUpdate()
        {
            VerticalSpeed = exitSpeed;
        }

        protected override void OnNoneEnter(FsmState prev, FsmTransition trans)
        {
            base.OnNoneEnter(prev, trans);
            VerticalSpeed = 0f;
        }

        #endregion
    }
}