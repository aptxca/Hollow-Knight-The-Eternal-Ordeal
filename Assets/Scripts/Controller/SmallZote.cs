using System;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Demo2D
{

    [Serializable]
    public class ZoteSimpleAi
    {
        public  float jumpPercent = 1f;
        public  ZoteState NextAct
        {
            get
            {
                if (Mathf.Approximately(jumpPercent,1f))
                {
                    return ZoteState.Jump;
                }

                return Random.Range(0f, 1f) > jumpPercent ? ZoteState.Attack : ZoteState.Jump;
            }
            
        }

    }




    public class SmallZote : Zote
    {
        public float jumpSpeed;
        public float maxHorizentalSpeed;
        public float acceleration;
        public float deceleration;
        public float maxCanTurnSpeed;
        public float idleTime =3f;
        public float maxAttackTime = 8f;


        private ZoteSimpleAi _zoteAi = new ZoteSimpleAi();

        private Vector2 _hitDir;
        private bool _fell =true;

        public override void InitFsm()
        {
            base.InitFsm();
            _zoteAi.jumpPercent = 0.8f;
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Attack))
                .AddState(new ZoteFsmState(ZoteState.Jump))
                .AddState(new ZoteFsmState(ZoteState.Falling))
                .AddState(new ZoteFsmState(ZoteState.Fell))
                .AddState(new ZoteFsmState(ZoteState.GetUp))
                .AddState(new ZoteFsmState(ZoteState.Landing))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.None))
                .AddAnyState(new ZoteFsmState(ZoteState.Any));
            var allFsmState = characterFsmSystem.States;

            allFsmState[ZoteState.Idle].RegisterProcess(OnIdleEnter, OnIdleExit)
                .AddTransition(new ZoteTransition(() => CurrentStateTime > idleTime && scanner.FindTarget&&IsGrounded&&_zoteAi.NextAct==ZoteState.Attack), allFsmState[ZoteState.Attack])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > idleTime && scanner.FindTarget&&IsGrounded&&_zoteAi.NextAct==ZoteState.Jump), allFsmState[ZoteState.Jump])
                .AddTransition(new ZoteTransition(() => !IsGrounded), allFsmState[ZoteState.Falling]);


            allFsmState[ZoteState.Attack].RegisterProcess(OnAttackEnter, OnAttackExit, OnAttackUpdate)
                //.AddTransition(new ZoteTransition(null, _damageable.damageTrigger), allFsmState[ZoteState.Hit])
                .AddTransition(new ZoteTransition(() => CurrentStateTime > CurrentStateFloatPara), allFsmState[ZoteState.Fell]);

            allFsmState[ZoteState.Falling].RegisterProcess(OnFallingEnter, null, OnFallingUpdate)
                .AddTransition(new ZoteTransition(() => IsGrounded&&!_fell), allFsmState[ZoteState.Landing])
                .AddTransition(new ZoteTransition(() => IsGrounded&&_fell&&VerticalSpeed<0f), allFsmState[ZoteState.Fell]);

            allFsmState[ZoteState.Landing].RegisterProcess(OnLandEnter)
                .AddTransition(new ZoteTransition(() => IsGrounded&& animator.IsCurrentStateEnd(0)), allFsmState[ZoteState.Idle]);

            allFsmState[ZoteState.GetUp].RegisterProcess(OnGetUpEnter)
                .AddTransition(new ZoteTransition(() => IsGrounded&&animator.IsCurrentStateEnd(0)), allFsmState[ZoteState.Idle]);

            allFsmState[ZoteState.Fell].RegisterProcess(OnFellEnter)
                .AddTransition(new ZoteTransition(() => IsGrounded&& allFsmState[ZoteState.Fell].Time>2f), allFsmState[ZoteState.GetUp]);

            allFsmState[ZoteState.Jump].RegisterProcess(OnJumpEnter,null,OnJumpUpdate)
                .AddTransition(new ZoteTransition(() => VerticalSpeed <= 0f), allFsmState[ZoteState.Falling]);

            allFsmState[ZoteState.Hit].RegisterProcess(OnHitEnter, OnHitExit)
                .AddTransition(new ZoteTransition(() => !IsDead && CurrentStateTime > 0.02f), allFsmState[ZoteState.Falling])
                .AddTransition(new ZoteTransition(() => IsDead), allFsmState[ZoteState.Dead]);

            allFsmState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(() => animator.IsCurrentStateEnd(0) && !effectModule.IsPlaying()), allFsmState[ZoteState.None]);

            allFsmState[ZoteState.None].RegisterProcess(OnNoneEnter);


            characterFsmSystem.AnyState.AddTransition(new ZoteTransition(null, damageable.damageTrigger),
                allFsmState[ZoteState.Hit]);


            characterFsmSystem.SetDefaultState(ZoteState.Idle);

        }


        #region 状态机回调
        protected override void OnIdleExit(FsmState next, FsmTransition trans)
        {
            if (next.EnumState.Equals(ZoteState.Attack))
            {
                trans.FloatPara = Random.Range(0.5f * maxAttackTime, maxAttackTime);
            }
        }

        protected override void OnJumpEnter(FsmState prev, FsmTransition trans)
        {
            base.OnJumpEnter(prev, trans);
            _fell = Random.Range(0f, 1f) > 0.5f;
            float speedScale = 0.5f;
            if (IsTargetAtLeft)
            {
                ChangeHeading(true);
                HorizontalSpeed = Random.Range(-maxHorizentalSpeed * speedScale, -maxHorizentalSpeed * speedScale * 0.4f);
            }
            else
            {
                ChangeHeading(false);

                HorizontalSpeed = Random.Range(maxHorizentalSpeed * speedScale * 0.4f, maxHorizentalSpeed * speedScale);
            }
            VerticalSpeed = Random.Range(0.5f * jumpSpeed, jumpSpeed);

        }

        protected override void OnFallingEnter(FsmState prev, FsmTransition trans)
        {

            if (prev.EnumState.Equals(ZoteState.Hit)&&trans.IntPara==1)
            {
                animator.Play("Roll_Bwd",0,0);
            }
            else
            {
                animator.Play("Roll_Fwd",0,0);
            }
            VerticalSpeed -= 1f;
            audioModule.PlayAudioRandom(AudioType.Falling);
        }

        protected override void OnAttackUpdate()
        {
            if (IsHeadingLeft)
            {
                if (IsTargetAtLeft)
                {
                    HorizontalSpeed = Mathf.MoveTowards(HorizontalSpeed, -maxHorizentalSpeed, acceleration * Time.deltaTime);
                }
                else
                {
                    HorizontalSpeed = Mathf.MoveTowards(HorizontalSpeed, 0, deceleration * Time.deltaTime);
                    if (HorizontalSpeed > -maxCanTurnSpeed)
                    {
                        ChangeHeading(false);
                    }
                }
            }
            else
            {
                if (!IsTargetAtLeft)
                {
                    HorizontalSpeed = Mathf.MoveTowards(HorizontalSpeed, maxHorizentalSpeed, acceleration * Time.deltaTime);
                }
                else
                {
                    HorizontalSpeed = Mathf.MoveTowards(HorizontalSpeed, 0, deceleration * Time.deltaTime);
                    if (HorizontalSpeed < maxCanTurnSpeed)
                    {
                        ChangeHeading(true);
                    }
                }
            }



        }

        protected override void OnHitEnter(FsmState prev, FsmTransition trans)
        {
            base.OnHitEnter(prev, trans);
            _fell = true;
            DamageTrigger trigger = trans.Trigger as DamageTrigger;
            float knockBack = trigger.Damage.knockBack;
            _hitDir = trigger.Damage.direction;
            HorizontalSpeed = (_hitDir * knockBack).x;
            VerticalSpeed = (_hitDir.y < 0 ? _hitDir : (_hitDir + Vector2.up * 0.5f) * knockBack).y;
        }


        protected override void OnHitExit(FsmState next, FsmTransition trans)
        {
            
            if (_hitDir==default)
            {
                trans.IntPara = 0;
                return;
            }
            //判断下一个状态的动作是后滚还是前滚，后滚0，前滚1
            if (IsHeadingLeft)
            {
                trans.IntPara = _hitDir.x>0 ? 0 : 1;
            }
            else
            {
                trans.IntPara = _hitDir.x > 0 ? 1 : 0;
            }
        }

        #endregion

    }
}