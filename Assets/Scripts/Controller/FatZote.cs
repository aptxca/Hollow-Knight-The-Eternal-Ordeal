using Demo2D.Frame;
using Demo2D.Utility;
using Random = UnityEngine.Random;

namespace Demo2D
{
    public class FatZote : Zote
    {
        public Shooter shooter;

        public float jumpSpeed;
        public float maxHorizentalSpeed;
        public float idleTime = 1f;




        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Jump))
                .AddState(new ZoteFsmState(ZoteState.Falling))
                .AddState(new ZoteFsmState(ZoteState.Landing))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None))
                .AddAnyState(new ZoteFsmState(ZoteState.Any));
            var allState = characterFsmSystem.States;


            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => !IsGrounded), allState[ZoteState.Falling])
                .AddTransition(new ZoteTransition(() => allState[ZoteState.Idle].Time > idleTime && IsGrounded), allState[ZoteState.Jump]);

            allState[ZoteState.Jump].RegisterProcess(OnJumpEnter, null, OnJumpUpdate)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => VerticalSpeed < 0f), allState[ZoteState.Falling]);

            allState[ZoteState.Falling].RegisterProcess(OnFallingEnter, null, OnFallingUpdate)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => IsGrounded), allState[ZoteState.Landing]);

            allState[ZoteState.Landing].RegisterProcess(OnLandEnter)
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead])
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0)), allState[ZoteState.Idle]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), allState[ZoteState.None]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            //??????????????????????????????????????????????????????????????????????????????????????????????????????????????????
            characterFsmSystem.RegisterAnyStateTrigger(damageable.damageTrigger, OnHitCallBack);

            //FIXME???????????????????????????????????????dead=>dead?????????
            //_characterFsmSystem.AnyState.AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead]);

            characterFsmSystem.SetDefaultState(ZoteState.Idle);
        }



        //??????????????????????????????????????????
        private void OnHitCallBack(ITrigger obj)
        {
            if (!IsDead)
            {
                audioModule.PlayAudioRandom(AudioType.Hit);
                audioModule.PlayVoiceRandom(AudioType.HitVoice);
                hitEffect.Play();
            }

        }



        #region ?????????????????????
        protected override void OnJumpEnter(FsmState prev, FsmTransition trans)
        {
            if (IsTargetAtLeft)
            {
                ChangeHeading(true);
                HorizontalSpeed = Random.Range(-maxHorizentalSpeed,
                    -maxHorizentalSpeed * 0.4f);
            }
            else
            {
                ChangeHeading(false);

                HorizontalSpeed = Random.Range(maxHorizentalSpeed * 0.4f, maxHorizentalSpeed);
            }

            VerticalSpeed = Random.Range(0.8f * jumpSpeed, jumpSpeed);
            base.OnJumpEnter(prev, trans);
        }

        protected override void OnLandEnter(FsmState prev, FsmTransition trans)
        {
            base.OnLandEnter(prev, trans);
            shooter.ShootProjectile(null,0);
            shooter.ShootProjectile(null,1);

        }

        #endregion



    }

}