using Demo2D.Frame;
using Demo2D.Utility;

namespace Demo2D
{
    public class VineZote : Zote
    {
        public Shooter shooter;

        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Entrance))
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Attack))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));

            var fsmStates = characterFsmSystem.States;


            fsmStates[ZoteState.Entrance].RegisterProcess(OnEntranceEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), fsmStates[ZoteState.Idle]);

            fsmStates[ZoteState.Idle].RegisterProcess(OnIdleEnter)
                .AddTransition(new ZoteTransition(() => scanner.FindTarget && CurrentStateTime > 3f), fsmStates[ZoteState.Attack])
                .AddTransition(new ZoteTransition(() => IsDead), fsmStates[ZoteState.Dead]);

            fsmStates[ZoteState.Attack].RegisterProcess(OnAttackEnter, OnAttackExit)
                .AddTransition(new ZoteTransition(IsAllStopped), fsmStates[ZoteState.Idle])
                .AddTransition(new ZoteTransition(() => IsDead), fsmStates[ZoteState.Dead]);

            fsmStates[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), fsmStates[ZoteState.None]);

            fsmStates[ZoteState.None].RegisterProcess(OnNoneEnter);


            characterFsmSystem.RegisterAnyStateTrigger(damageable.damageTrigger, OnHitCallBack);

            characterFsmSystem.SetDefaultState(ZoteState.Entrance);


        }

        //任何状态下受击都会调用的回调
        private void OnHitCallBack(ITrigger obj)
        {
            if (!IsDead)
            {
                audioModule.PlayAudioRandom(AudioType.Hit);
                hitEffect?.Play();
            }

        }

        #region 状态机相关

        protected override void OnIdleEnter(FsmState prev, FsmTransition next)
        {
            animator?.Play("Idle", 0, 0);
        }

        protected override void OnAttackEnter(FsmState arg1, FsmTransition arg2)
        {
            animator?.Play("Attack", 0, 0);
        }

        protected override void OnAttackExit(FsmState arg1, FsmTransition arg2)
        {
            shooter.ShootProjectile(scanner.target.gameObject,0);
            audioModule.PlayAudio(AudioType.Spit);
        }

        #endregion

    }
}