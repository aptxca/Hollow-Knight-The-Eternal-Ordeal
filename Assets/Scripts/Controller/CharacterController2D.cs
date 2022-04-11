using System;
using Demo2D.Frame;
using UnityEngine;

namespace Demo2D
{
    [RequireComponent(typeof(Animator),typeof(Damageable))]
    public abstract class CharaterController2D : MonoBehaviour
    {
        public MonoAudioModule audioModule;
        public MonoEffectModule effectModule;
        public HitEffect hitEffect;
        public float gravity;
        protected Collider2D[] bodyCollider;

        //角色的Animator
        protected Animator animator;

        //换武器时修改动作要用到的
        public AnimatorOverrideController overrideController;
        
        //武器
        public Sword sword;
        //伤害脚本
        protected Damageable damageable;
        //角色状态机
        protected FsmSystem characterFsmSystem;

        protected Action _deathEvent;

        public float CurrentStateTime => characterFsmSystem.CurrentState.Time;

        public float CurrentStateFloatPara
        {
            get => characterFsmSystem.CurrentState.FloatPara;
            set => characterFsmSystem.CurrentState.FloatPara = value;
        }
        public bool IsDead => damageable.currentStateInfo.hp.Data == 0;

        public virtual void Init()
        {
            damageable = GetComponent<Damageable>();
            animator = GetComponent<Animator>();
            _rigidbody = GetComponent<Rigidbody2D>();
            animator.applyRootMotion = false;
            overrideController = AnimatorInit(animator);
            bodyCollider = GetComponents<Collider2D>();
        }

        public abstract bool IsGrounded { get;}

        protected abstract Platform Plat { get;}

        protected  float HorizontalSpeed { get; set; }

        protected  float VerticalSpeed { get; set; }

        protected Rigidbody2D _rigidbody;

        protected void ChangeHeading(bool left)
        {
            transform.rotation = Quaternion.Euler(0, left ? 0 : 180, 0);
        }

        private void FixedUpdate()
        {
            UpdateMove();
        }

        protected void UpdateMove()
        {
            Vector2 speed = Vector2.zero;

            if (IsGrounded && Plat != null)
            {
                speed.x = HorizontalSpeed + Plat.surfaceSpeed + Plat.CurrentPlatSpeed.x;
                speed.y = VerticalSpeed + Plat.CurrentPlatSpeed.y;
            }
            else
            {
                speed.x = HorizontalSpeed;
                speed.y = VerticalSpeed;
            }
            if (_rigidbody!=null)
            {
                _rigidbody.MovePosition(_rigidbody.position + speed * Time.fixedDeltaTime);
            }
            else
            {
                transform.Translate(speed * Time.deltaTime, Space.World);
            }

        }


        /// <summary>
        /// 播放一个动作
        /// </summary>
        /// <param name="name">动作名称</param>
        public virtual void ApplyAct(string name, int index = 0,float delay = 0)
        {
            animator.Play(name,-1,0);
        }

        public virtual AnimatorOverrideController AnimatorInit(Animator animator)
        {
            AnimatorOverrideController overrideController =
                new AnimatorOverrideController(animator.runtimeAnimatorController);
            animator.runtimeAnimatorController = overrideController;

            return overrideController;
        }

        public void RegisterDeathEvent(Action callback)
        {
            _deathEvent += callback;
        }

        public void UnRegisterDeathEvent(Action callback)
        {
            _deathEvent -= callback;
        }


        #region 状态机相关回调

        protected virtual void OnEntranceEnter(FsmState prev, FsmTransition trans)
        {
            //Debug.Log("Default Entrance");
            animator.Play("Entrance", 0, 0);
        }

        protected virtual void OnEntranceUpdate()
        {
            Debug.Log("OnEntranceUpdate not implemented");
        }

        protected virtual void OnIdleEnter(FsmState prev, FsmTransition trans)
        {
            //Debug.Log("Default Idle");
            ApplyAct("Idle");

            if (audioModule!=null)
            {
                if (!audioModule.IsPlaying(AudioType.Idle))
                {
                    audioModule.PlayAudioRandom(AudioType.Idle);
                }

                if (!audioModule.IsPlaying(AudioType.IdleVoice))
                {
                    audioModule.PlayVoiceRandom(AudioType.IdleVoice);
                }
            }

            if (VerticalSpeed < -1f)
            {
                VerticalSpeed = -1f;
            }

            HorizontalSpeed = 0f;
        }

        protected virtual void OnIdleUpdate()
        {
            Debug.Log("OnIdleUpdate not implemented");
        }

        protected virtual void OnIdleExit(FsmState next, FsmTransition trans)
        {
            audioModule.StopPlay(AudioType.Idle);
        }

        protected virtual void OnRunEnter(FsmState prev, FsmTransition trans)
        {
            ApplyAct("Run");
            audioModule.PlayAudio(AudioType.FootSteps);
        }

        protected virtual void OnRunExit(FsmState next, FsmTransition trans)
        {
            audioModule.StopPlay(AudioType.FootSteps);
        }

        protected virtual void OnRunUpdate()
        {
            Debug.Log("OnRunUpdate not implemented");
        }

        protected virtual void OnAttackEnter(FsmState prev, FsmTransition trans)
        {
            CurrentStateFloatPara = trans.FloatPara;
            animator.Play("Attack", 0, 0);
            if (audioModule!=null)
            {
                audioModule.PlayAudioRandom(AudioType.Attack);
                audioModule.PlayVoiceRandom(AudioType.AttackVoice);
            }

        }

        protected virtual void OnAttackUpdate()
        {
            Debug.Log("OnAttackUpdate not implemented");
        }

        protected virtual void OnAttackExit(FsmState next, FsmTransition trans)
        {
            audioModule.StopPlay(AudioType.Attack);
            audioModule.StopPlay(AudioType.AttackVoice);
        }

        protected virtual void OnFellEnter(FsmState prev, FsmTransition trans)
        {
            HorizontalSpeed = 0f;
            animator.Play("Fell", 0, 0);
            audioModule.PlayAudioRandom(AudioType.Fell);
            audioModule.PlayVoiceRandom(AudioType.FellVoice);

        }

        protected virtual void OnGetUpEnter(FsmState prev, FsmTransition trans)
        {
            animator.Play("GetUp", 0, 0);
            audioModule.PlayAudioRandom(AudioType.GetUp);
        }

        protected virtual void OnJumpEnter(FsmState prev, FsmTransition trans)
        {
            //Debug.Log("Default OnJumpEnter");
            animator.Play("Jump", 0, 0);
            audioModule.PlayAudioRandom(AudioType.Jump);
            audioModule.PlayVoiceRandom(AudioType.JumpVoice);
        }

        protected virtual void OnJumpUpdate()
        {
            //Debug.Log("Default OnJumpUpdate");
            VerticalSpeed -= gravity * Time.deltaTime;
        }

        protected virtual void OnJumpExit(FsmState next, FsmTransition trans)
        {
            Debug.Log("OnJumpExit not implemented");
        }

        protected virtual void OnFallingEnter(FsmState prev, FsmTransition trans)
        {
            //Debug.Log("Default OnFallingEnter");
            animator.Play("Falling", 0, 0);
            VerticalSpeed -= 1f;
            audioModule.PlayAudioRandom(AudioType.Falling);
        }

        protected virtual void OnFallingUpdate()
        {
            //Debug.Log("Default OnFallingUpdate");
            VerticalSpeed -= gravity * Time.deltaTime;
            if (VerticalSpeed < -15f)
            {
                VerticalSpeed = -15f;
            }
        }

        protected virtual void OnFallingExit(FsmState next, FsmTransition trans)
        {
            //Debug.Log("Default OnFallingEnter");
            audioModule.StopPlay(AudioType.Falling);
        }

        protected virtual void OnLandEnter(FsmState prev, FsmTransition trans)
        {
            //Debug.Log("Default OnLandEnter");
            HorizontalSpeed = 0f;
            VerticalSpeed = -1f;
            animator.Play("Land", 0, 0);
            audioModule.PlayAudioRandom(AudioType.Land);
            if (effectModule !=null)
            {
                effectModule.Play(EffectType.LandDust, 0);
            }
        }

        protected virtual void OnLandUpdate()
        {
            Debug.Log("OnLandUpdate not implemented");
        }



        protected virtual void OnHitEnter(FsmState prev, FsmTransition trans)
        {
            animator.Play("Hit", 0, 0);
            if (audioModule!=null)
            {
                audioModule.PlayAudioRandom(AudioType.Hit);
                audioModule.PlayVoiceRandom(AudioType.HitVoice);
            }

            if (hitEffect !=null)
            {
                hitEffect.Play();
            }

        }

        protected virtual void OnHitUpdate()
        {

        }

        protected virtual void OnHitExit(FsmState next, FsmTransition trans)
        {
            Debug.Log("OnHitExit not implemented");
        }

        protected virtual void OnDeadEnter(FsmState prev, FsmTransition trans)
        {
            _deathEvent?.Invoke();
            animator.Play("Death", 0, 0);
            if (audioModule!=null)
            {
                audioModule.PlayAudioRandom(AudioType.Dead);
                audioModule.PlayVoiceRandom(AudioType.DeadVoice);
            }
            if (effectModule!=null)
            {
                effectModule.Play(EffectType.Death, 0);
            }
            foreach (var collider in bodyCollider)
            {
                collider.enabled = false;
            }
            HorizontalSpeed = 0f;
            VerticalSpeed = 0f;
        }

        protected virtual void OnDeadExit(FsmState next, FsmTransition trans)
        {
            Debug.Log("OnDeadExit not implemented");
        }

        protected virtual void OnLeaveUpdate()
        {
            Debug.Log("OnLeaveUpdate not implemented");
        }

        protected virtual void OnNoneEnter(FsmState prev, FsmTransition trans)
        {
            HorizontalSpeed = 0f;
            VerticalSpeed = 0f;
            GameObjectManager.Instance.Recycle(gameObject);
        }

        #endregion

        #region 动画事件回调
        //动画片段开始事件
        protected virtual void AnimStart()
        {

        }

        //动画片段结束事件
        protected virtual void AnimEnd(AnimationEvent evt)
        {

        }

        //角色的状态更改事件
        protected virtual void AnimState(string type)
        {

        }

        //音效事件
        protected virtual void AnimSound(string audioId)
        {

        }

        //移动事件
        protected virtual void AnimMove(string jsonMove)
        {

        }
        #endregion
    }
}