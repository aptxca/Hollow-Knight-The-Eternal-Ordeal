using Demo2D.Frame;
using UnityEngine;
using System;
using Demo2D.Utility;

namespace Demo2D
{
    public class BombZote : Zote
    {
        public float countdown;
        public string explosionPath;
        public GameObject explosion;
        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Entrance))
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Attack))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.None));

            var allState = characterFsmSystem.States;
            //entrance
            allState[ZoteState.Entrance].RegisterProcess(OnEntranceEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.Idle]);
            //idle
            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > countdown), allState[ZoteState.Attack]);
            //attack
            allState[ZoteState.Attack].RegisterProcess(OnAttackEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.Dead]);
            //death
            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), allState[ZoteState.None]);
            //none
            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            characterFsmSystem.SetDefaultState(ZoteState.Entrance);
        }


        #region 状态机回调函数

        protected override void OnDeadEnter(FsmState prev, FsmTransition trans)
        {
            base.OnDeadEnter(prev, trans);
            explosion = GameObjectManager.Instance.InstantiateGameObject(explosionPath);
            explosion.transform.position = transform.position;
        }

        #endregion

    }
}