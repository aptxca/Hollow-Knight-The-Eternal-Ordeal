using System;
using System.Collections;
using System.Collections.Generic;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    [DefaultExecutionOrder(1000)]
    public class PlayerController : CharaterController2D
    {
        private static PlayerController _instance;
        public static PlayerController Instance => _instance;

        //角色持有的组件
        public GroundDetector groundDetector;

        [Header("运动相关")]
        public float maxForwardSpeed = 8;
        public float maxFallingSpeed = 10;
        public float dashSpeed = 40;
        public float takeOffSpeed = 10f;


        [Header("战斗相关")]
        public float healInterval = 0.8f;
        public Vector2 knockbackSpeed;
        public float attackChargeDelay;
        public float attackDownJumpSpeed;


        public bool IsHeadingLeft => transform.rotation.y == 0;
        public override bool IsGrounded => groundDetector.IsGrounded;
        protected override Platform Plat => groundDetector.Plat;
        private bool CanDoubleJump { get; set; }
        private bool CanDash { get; set; }
        private bool IsMoveInput => !Mathf.Approximately(MoveInput, 0);
        private bool IsLookDown => LookInput < -0.6f;
        private bool IsLookUp => LookInput > 0.6f;
        private bool IsLookForward => !IsLookDown && !IsLookUp;
        private float LookInput => PlayerInput.Instance.LookInput;
        private float MoveInput => PlayerInput.Instance.MoveInput;

        public List<Interactable> Interactables { get; protected set; } = new List<Interactable>();

        //Animator参数
        private int _hashLookInput = Animator.StringToHash("LookInput");
        private int _hashVerticalSpeed = Animator.StringToHash("VerticalSpeed");
        //内部参数
        private float _focusTime;
        private bool _swordArtReady = false;
        private bool _jumpReady = true;



        private EffectFsmSystem _effectSystem;

        void Awake()
        {
            Init();
        }

        public override void Init()
        {
            if (Instance == null)
            {
                _instance = this;
            }
            if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            base.Init();
            DontDestroyOnLoad(gameObject);
            InitFsm();
            InitEffectFsm();
            UIManager.Instance.RegisterPlayerStatus(damageable);
            //RegisterDeathEvent(GameSceneManager.Instance.OnHeroKilled);
        }


        void OnEnable()
        {
            if (characterFsmSystem.CurrentState != null)
            {
                characterFsmSystem.ReStart();
            }

            if (_effectSystem.CurrentState != null)
            {
                _effectSystem.ReStart();
            }
        }

        void Update()
        {
            characterFsmSystem.OnUpdate();
            _effectSystem.OnUpdate();
            if (IsGrounded)
            {
                CanDoubleJump = true;
                CanDash = true;
            }

            for (int i = 0; i < Interactables.Count; i++)
            {
                Interactable interactable = Interactables[i];
                if (interactable.CheckCondition())
                {
                    interactable.Interact();
                }
            }
        }



        private PlayerTrigger GetPlayerTrigger(string name)
        {
            return PlayerInput.Instance.GetPlayerTrigger(name);
        }

        private bool InputButtonState(string name, ButtonState state, float holdTime = 0f)
        {
            InputButton button = PlayerInput.Instance.GetInputButton(name);
            if (button == null)
            {
                return false;
            }

            if (holdTime < 0.02f)
            {
                return button.State == state;
            }

            return button.State == state && button.holdTime >= holdTime;
        }

        public void Respawn(Vector2 position, bool resetDamage)
        {
            transform.position = position;
            ResetStatus(resetDamage);
            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
        }

        public void ResetStatus(bool resetDamage = true)
        {
            if (resetDamage)
            {
                damageable.ResetDamage();
            }

            if (gameObject.layer ==LayerMask.NameToLayer("Invulnerable"))
            {
                gameObject.layer = LayerMask.NameToLayer("Player");
            }

            foreach (var collider in bodyCollider)
            {
                if (collider.enabled ==false)
                {
                    collider.enabled = true;
                }
            }

            if (groundDetector!=null)
            {
                groundDetector.ResetStatus();
            }

            if (characterFsmSystem.CurrentState != null)
            {
                characterFsmSystem.ReStart();
            }

            if (_effectSystem.CurrentState != null)
            {
                _effectSystem.ReStart();
            }

        }

        public void FindInteractable(Interactable interactable)
        {
            Interactables.Add(interactable);
        }

        public void LoseInteractable(Interactable interactable)
        {
            Interactables.Remove(interactable);
        }

        /// <summary>
        /// 角色状态机初始化
        /// </summary>
        private void InitFsm()
        {
            characterFsmSystem = new PlayerFsmSystem();

            //根据PlayerState枚举实例化所有的PlayerFSMState
            var allStates = Enum.GetValues(typeof(PlayerState));
            for (int i = 0; i < allStates.Length; i++)
            {
                PlayerState state = (PlayerState) allStates.GetValue(i);
                characterFsmSystem.AddState(new PlayerFsmState(state));
            }

            Dictionary<Enum, FsmState> stmStates = characterFsmSystem.States;
            characterFsmSystem.SetDefaultState(stmStates[PlayerState.Idle]);




            //idle状态
            stmStates[PlayerState.Idle]
                //注册进入、离开、正在idle状态的方法
                .RegisterProcess(OnIdleEnter, null, OnIdleUpdate)
                //idle => run
                .AddTransition(new PlayerTransition(() => IsMoveInput), stmStates[PlayerState.Run])
                //idle => airborne
                .AddTransition(new PlayerTransition(() => !IsGrounded), stmStates[PlayerState.Falling])
                //idle =>jump
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Jump")), stmStates[PlayerState.Jump])
                //.AddTransition(new PlayerTransition(()=>InputButtonState("Jump",ButtonState.Down)), stmStates[PlayerState.Jump])
                //idle=>dash
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Dash")), stmStates[PlayerState.Dash])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("DashJoystick")), stmStates[PlayerState.Dash])
                //idle => Attack,地面上攻击的时候不允许打出下劈
                .AddTransition(new PlayerTransition(() => LookInput > -0.5f, GetPlayerTrigger("Attack"), para:0), stmStates[PlayerState.Attack])
                //蓄力劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady && IsLookForward, GetPlayerTrigger("AttackRelease"), para:1), stmStates[PlayerState.Attack])
                //大风车
                .AddTransition(new PlayerTransition(() => _swordArtReady && !IsLookForward, GetPlayerTrigger("AttackRelease"),para:2), stmStates[PlayerState.Attack])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Focus")), stmStates[PlayerState.Focus])
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);


            //Run状态
            stmStates[PlayerState.Run]
                .RegisterProcess(OnRunEnter, OnRunExit, OnRunUpdate)
                .AddTransition(new PlayerTransition(() => !IsMoveInput), stmStates[PlayerState.Idle])
                //idle =>jump
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Jump")), stmStates[PlayerState.Jump])
                //idle=>dash
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Dash")), stmStates[PlayerState.Dash])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("DashJoystick")), stmStates[PlayerState.Dash])
                //idle=>falling
                .AddTransition(new PlayerTransition(() => !IsGrounded), stmStates[PlayerState.Falling])

                //idle => Attack,地面上攻击的时候不允许打出下劈
                .AddTransition(new PlayerTransition(() => LookInput > -0.5f, GetPlayerTrigger("Attack"), para: 0), stmStates[PlayerState.Attack])
                //蓄力劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady && IsLookForward, GetPlayerTrigger("AttackRelease"), para: 1), stmStates[PlayerState.Attack])
                //大风车
                .AddTransition(new PlayerTransition(() => _swordArtReady && !IsLookForward, GetPlayerTrigger("AttackRelease"), para: 2), stmStates[PlayerState.Attack])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Focus")), stmStates[PlayerState.Focus])
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //Jump状态
            stmStates[PlayerState.Jump].RegisterProcess(OnJumpEnter, OnJumpExit,OnJumpUpdate)
                //jump=>falling
                .AddTransition(new PlayerTransition(() => VerticalSpeed <= 0f), stmStates[PlayerState.Falling])
                .AddTransition(new PlayerTransition(null,GetPlayerTrigger("JumpAbort")), stmStates[PlayerState.Falling])
                //普通攻击
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Attack"),true,0), stmStates[PlayerState.Attack])
                //蓄力劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady && IsLookForward, GetPlayerTrigger("AttackRelease"), para: 1), stmStates[PlayerState.Attack])
                //大风车
                .AddTransition(new PlayerTransition(() => _swordArtReady && !IsLookForward, GetPlayerTrigger("AttackRelease"), para: 2), stmStates[PlayerState.Attack])
                //idle=>dash
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("Dash")), stmStates[PlayerState.Dash])
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("DashJoystick")), stmStates[PlayerState.Dash])

                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //Falling
            stmStates[PlayerState.Falling]
                .RegisterProcess(OnFallingEnter, OnFallingExit, OnFallingUpdate)
                .AddTransition(new PlayerTransition(() => IsGrounded), stmStates[PlayerState.Landing])
                //普通攻击
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Attack"), true, 0), stmStates[PlayerState.Attack])
                //蓄力劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady && IsLookForward, GetPlayerTrigger("AttackRelease"), para: 1), stmStates[PlayerState.Attack])
                //大风车
                .AddTransition(new PlayerTransition(() => _swordArtReady && !IsLookForward, GetPlayerTrigger("AttackRelease"), para: 2), stmStates[PlayerState.Attack])
                //falling => dash
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("Dash")), stmStates[PlayerState.Dash])
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("DashJoystick")), stmStates[PlayerState.Dash])

                //符合二段跳条件才允许起跳
                .AddTransition(new PlayerTransition(() => CanDoubleJump && _jumpReady, GetPlayerTrigger("Jump")), stmStates[PlayerState.DoubleJump])
                //falling=>hit
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //doubleJump
            stmStates[PlayerState.DoubleJump].RegisterProcess(OnDoubleJumpEnter, OnDoubleJumpExit, OnDoubleJumpUpdate)
                //普通攻击
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Attack"), true, 0), stmStates[PlayerState.Attack])
                //蓄力劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady && IsLookForward, GetPlayerTrigger("AttackRelease"), para: 1), stmStates[PlayerState.Attack])
                //大风车
                .AddTransition(new PlayerTransition(() => _swordArtReady && !IsLookForward, GetPlayerTrigger("AttackRelease"), para: 2), stmStates[PlayerState.Attack])
                //doubleJump => dash
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("Dash")), stmStates[PlayerState.Dash])
                .AddTransition(new PlayerTransition(() => CanDash, GetPlayerTrigger("DashJoystick")), stmStates[PlayerState.Dash])
                //doubleJump => falling
                .AddTransition(new PlayerTransition(() => VerticalSpeed <= 0f), stmStates[PlayerState.Falling])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("JumpAbort")), stmStates[PlayerState.Falling])
                //doubleJump => hit
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //Landing
            stmStates[PlayerState.Landing].RegisterProcess(OnLandEnter)
                .AddTransition(new PlayerTransition(() => animator.IsCurrentStateEnd(0)&&IsGrounded && !IsMoveInput), stmStates[PlayerState.Idle])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Jump")), stmStates[PlayerState.Jump])
                .AddTransition(new PlayerTransition(() => IsGrounded && IsMoveInput), stmStates[PlayerState.Run]);

            //attack
            stmStates[PlayerState.Attack]
                .RegisterProcess(OnAttackEnter, OnAttackExit, OnAttackUpdate)
                //attack => idle
                .AddTransition(new PlayerTransition(() => animator.IsCurrentStateEnd(0) && IsGrounded && !IsMoveInput), stmStates[PlayerState.Idle])
                //attack => run
                .AddTransition(new PlayerTransition(() => animator.IsCurrentStateEnd(0) && IsGrounded && IsMoveInput), stmStates[PlayerState.Run])
                //attack => airborne
                .AddTransition(new PlayerTransition(() => animator.IsCurrentStateEnd(0) && !IsGrounded), stmStates[PlayerState.Falling])
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //Focus
            stmStates[PlayerState.Focus]
                .RegisterProcess(OnFocusEnter, OnFocusExit, OnFocusUpdate)
                .AddTransition(new PlayerTransition(null,GetPlayerTrigger("Spell")), stmStates[PlayerState.Idle])
                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);

            //dash
            stmStates[PlayerState.Dash]
                .RegisterProcess(OnDashEnter, OnDashExit)
                //dash => idle
                .AddTransition(new PlayerTransition(() => IsGrounded && !IsMoveInput && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Idle])
                //dash => run
                .AddTransition(new PlayerTransition(() => IsGrounded && IsMoveInput && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Run])
                //dash => falling
                .AddTransition(new PlayerTransition(() => !IsGrounded && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Falling])
                //冲刺劈砍
                .AddTransition(new PlayerTransition(() => _swordArtReady, GetPlayerTrigger("AttackRelease"),para:3), stmStates[PlayerState.Attack])

                .AddTransition(new PlayerTransition(null, damageable.damageTrigger), stmStates[PlayerState.Hit]);


            stmStates[PlayerState.Hit]
                .RegisterProcess(OnHitEnter, null, OnHitUpdate)
                //hit => idle
                .AddTransition(new PlayerTransition(() => IsGrounded && !IsMoveInput && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Idle])
                //hit => run
                .AddTransition(new PlayerTransition(() => IsGrounded && IsMoveInput && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Run])
                //hit => falling
                .AddTransition(new PlayerTransition(() => !IsGrounded && animator.IsCurrentStateEnd(0)), stmStates[PlayerState.Falling])
                //hit=>death
                .AddTransition(new PlayerTransition(() => IsDead), stmStates[PlayerState.Death]);

            stmStates[PlayerState.Death]
                .RegisterProcess(OnDeadEnter, OnDeadExit);

            characterFsmSystem.SetDefaultState(PlayerState.Idle);

        }

        /// <summary>
        /// 角色周身特效状态机初始化
        /// </summary>
        protected void InitEffectFsm()
        {
            _effectSystem = new EffectFsmSystem();
            Array allEnum = Enum.GetValues(typeof(PlayerEffectState));
            foreach (var state in allEnum)
            {
                _effectSystem.AddState(new EffectFsmState((PlayerEffectState)state));
            }

            var effectStates = _effectSystem.States;
            _effectSystem.SetDefaultState(PlayerEffectState.None);

            //none
            effectStates[PlayerEffectState.None]
                .RegisterProcess((state, transition) => ApplyAct("None_Effect"))
                .AddTransition(new PlayerTransition(()=>InputButtonState("Attack", ButtonState.Press, attackChargeDelay)), effectStates[PlayerEffectState.AttackChargeInit])
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("SuperDashCharge")), effectStates[PlayerEffectState.SuperDashChargeInit])
                //.AddTransition(new PlayerTransition(() => IsGrounded, GetPlayerTrigger("Focus")), effectStates[PlayerEffectState.FocusHeal])
                .AddTransition(new PlayerTransition(() => damageable.IsInvulnerable), effectStates[PlayerEffectState.Invulnerable]);
            //attackCharge
            effectStates[PlayerEffectState.AttackChargeInit].RegisterProcess(OnEffectAttackChargeInitEnter,OnEffectAttackChargeInitExit)
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("AttackRelease")), effectStates[PlayerEffectState.None])
                .AddTransition(new PlayerTransition(()=>animator.IsCurrentStateEnd(1)), effectStates[PlayerEffectState.AttackChargeDone]);

            effectStates[PlayerEffectState.AttackChargeDone].RegisterProcess(OnEffectAttackChargeDoneEnter)
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("AttackRelease")), effectStates[PlayerEffectState.None])
                .AddTransition(new PlayerTransition(() => animator.IsCurrentStateEnd(1)), effectStates[PlayerEffectState.AttackChargeLoop]);

            effectStates[PlayerEffectState.AttackChargeLoop].RegisterProcess(OnEffectAttackChargeLoopEnter)
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("AttackRelease")), effectStates[PlayerEffectState.None]);
            //SuperDashCharge
            effectStates[PlayerEffectState.SuperDashChargeInit].RegisterProcess(OnEffectSuperDashChargeInitEnter, null, () => Debug.Log("dash charging"))
                .AddTransition(new PlayerTransition(null, GetPlayerTrigger("SuperDashRelease")), effectStates[PlayerEffectState.None]);
            ////focus
            //effectStates[PlayerEffectState.FocusHeal]
            //    .RegisterProcess(OnEffectFocusHealEnter, null, OnEffectFocusHealUpdate)
            //    .AddTransition(new PlayerTransition(null, GetPlayerTrigger("Spell")), effectStates[PlayerEffectState.None]);

            effectStates[PlayerEffectState.Invulnerable]
                .RegisterProcess(OnEffectInvulnerableEnter, OnEffectInvulnerableExit, null)
                .AddTransition(new PlayerTransition(() => !damageable.IsInvulnerable), effectStates[PlayerEffectState.None]);

            
        }


        #region 角色特效的状态机回调

        private void OnEffectInvulnerableExit(FsmState arg1, FsmTransition arg2)
        {
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

        private void OnEffectInvulnerableEnter(FsmState arg1, FsmTransition arg2)
        {
            ApplyAct("Invulnerable_Blink");
        }


        private void OnEffectAttackChargeLoopEnter(FsmState arg1, FsmTransition arg2)
        {
            _swordArtReady = true;
            ApplyAct("Attack_Charge_Loop");
            audioModule.PlayAudio(AudioType.SwordCharge, 2);
        }



        private void OnEffectAttackChargeDoneEnter(FsmState arg1, FsmTransition arg2)
        {
            ApplyAct("Attack_Charge_Done");
            audioModule.PlayAudio(AudioType.SwordCharge, 1);
        }


        private void OnEffectAttackChargeInitEnter(FsmState arg1, FsmTransition arg2)
        {
            ApplyAct("Attack_Charge_Init");
            audioModule.PlayAudio(AudioType.SwordCharge, 0);
        }

        private void OnEffectAttackChargeInitExit(FsmState arg1, FsmTransition arg2)
        {
            audioModule.StopPlay(AudioType.SwordCharge);
        }

        private void OnEffectSuperDashChargeInitEnter(FsmState arg1, FsmTransition arg2)
        {
            Debug.Log("superdash charge enter");

        }

        #endregion

        #region 状态机回调

        protected override void OnIdleUpdate()
        {
            animator.SetFloat(_hashLookInput, LookInput);
        }

        protected override void OnRunUpdate()
        {
            float move = MoveInput;
            HorizontalSpeed = MoveInput * maxForwardSpeed;
            if (!Mathf.Approximately(move, 0f))
            {
                transform.rotation = Quaternion.Euler(0, move < 0 ? 0 : 180, 0);
            }
        }

        protected override void OnRunExit(FsmState next, FsmTransition trans)
        {
            audioModule.StopPlay(AudioType.FootSteps);
        }

        protected override void OnJumpEnter(FsmState prev, FsmTransition trans)
        {
            base.OnJumpEnter(prev, trans);
            VerticalSpeed = takeOffSpeed;
        }

        protected override void OnJumpUpdate()
        {
            base.OnJumpUpdate();
            float move = MoveInput;
            HorizontalSpeed = move * maxForwardSpeed;
            if (!Mathf.Approximately(move, 0f))
            {
                transform.rotation = Quaternion.Euler(0, move < 0 ? 0 : 180, 0);
            }

        }

        protected override void OnJumpExit(FsmState next, FsmTransition trans)
        {
            //如果退出的时候已经小于0了就不需要置零，比如：撞到天花板后反弹直接给了一个向下的速度
            if (VerticalSpeed > 0f)
            {
                VerticalSpeed = 0f;
            }
        }

        protected override void OnFallingUpdate()
        {
            float move = MoveInput;
            HorizontalSpeed = move * maxForwardSpeed;
            VerticalSpeed -= gravity * Time.deltaTime;
            if (VerticalSpeed <= -maxFallingSpeed)
            {
                VerticalSpeed = -maxFallingSpeed;
            }
            if (!Mathf.Approximately(move, 0f))
            {
                transform.rotation = Quaternion.Euler(0, move < 0 ? 0 : 180, 0);
            }
            animator.SetFloat(_hashVerticalSpeed, VerticalSpeed);
        }

        private void OnDoubleJumpEnter(FsmState prev, FsmTransition trans)
        {
            ApplyAct("DoubleJump");
            audioModule.PlayAudioRandom(AudioType.DoubleJump);
            VerticalSpeed = takeOffSpeed;
        }

        private void OnDoubleJumpUpdate()
        {
            float move = MoveInput;
            HorizontalSpeed = move * maxForwardSpeed;
            VerticalSpeed = Mathf.MoveTowards(VerticalSpeed, 0f, gravity * Time.deltaTime);
            if (!Mathf.Approximately(move, 0f))
            {
                transform.rotation = Quaternion.Euler(0, move < 0 ? 0 : 180, 0);
            }

        }

        private void OnDoubleJumpExit(FsmState next, FsmTransition trans)
        {
            //jumpCount--;
            //如果退出的时候已经小于0了就不需要置零，比如：撞到天花板后反弹直接给了一个向下的速度
            CanDoubleJump = false;
            CanDash = true;
            if (VerticalSpeed > 0f)
            {
                VerticalSpeed = 0f;
            }
        }


        protected override void OnAttackEnter(FsmState prev, FsmTransition trans)
        {
            animator.SetFloat(_hashLookInput, LookInput);

            int swordArtIndex = trans.IntPara;
            switch (swordArtIndex)
            {
                case 0:
                    if (IsLookDown)
                    {
                        ApplyAct("Attack_Down");
                    }
                    else if (IsLookUp)
                    {
                        ApplyAct("Attack_Up");
                    }
                    else
                    {
                        ApplyAct("Attack_Fwd");
                    }
                    break;
                case 1:
                    VerticalSpeed = 0f;
                    ApplyAct("SwordArt 1");
                    break;
                case 2:
                    ApplyAct("SwordArt 2");
                    break;
                case 3:
                    VerticalSpeed = 0f;
                    ApplyAct("SwordArt 3");
                    break;
                default:
                    Debug.Log("传给attack状态的参数错误：" + swordArtIndex);
                    return;
            }
            sword?.Attack(LookInput, swordArtIndex);
            _swordArtReady = false;
        }

        protected override void OnAttackUpdate()
        {
            HorizontalSpeed = MoveInput * maxForwardSpeed;
            if (sword.HitSomething)
            {
                Vector2 feedback = sword.HitFeedback;
                if (feedback.y>0.5f)
                {
                    VerticalSpeed = attackDownJumpSpeed;
                    CanDash = true;
                    CanDoubleJump = true;
                }

                if (feedback.x<-0.5f||feedback.x>0.5f)
                {
                     HorizontalSpeed += sword.HitFeedback.x;
                }
            }
        }

        protected override void OnAttackExit(FsmState next, FsmTransition trans)
        {
            base.OnAttackExit(next, trans);
            sword.HitSomething = false;
        }

        private void OnFocusEnter(FsmState prev, FsmTransition trans)
        {
            HorizontalSpeed = 0f;
            ApplyAct("Focus");
            ApplyAct("Focus_Heal",1);
            _focusTime = 0f;
            if (audioModule != null)
            {
                audioModule.PlayAudio(AudioType.Focus);
            }
        }

        private void OnFocusUpdate()
        {
            _focusTime += Time.deltaTime;
            if (_focusTime > healInterval)
            {
                ApplyAct("Heal_Done", 2);
                damageable.Heal(1);
                if (audioModule!=null)
                {
                    audioModule.PlayAudio(AudioType.Heal);
                }
                _focusTime = 0f;
            }
        }

        private void OnFocusExit(FsmState next, FsmTransition trans)
        {
            ApplyAct("None_Effect",1);
            if (audioModule != null)
            {
                audioModule.StopPlay(AudioType.Focus);
            }
        }

        private void OnDashEnter(FsmState prev, FsmTransition trans)
        {
            HorizontalSpeed = IsHeadingLeft ? -dashSpeed : dashSpeed;
            VerticalSpeed = 0f;
            ApplyAct("Dash");
            gameObject.layer = LayerMask.NameToLayer("Invulnerable");
            audioModule.PlayAudioRandom(AudioType.Dash);
        }

        private void OnDashExit(FsmState next, FsmTransition trans)
        {

            HorizontalSpeed = 0;
            CanDash = false;
            gameObject.layer = LayerMask.NameToLayer("Player");
        }

        protected override void OnHitEnter(FsmState prev, FsmTransition trans)
        {
            base.OnHitEnter(prev, trans);

            DamageTrigger trigger = trans.Trigger as DamageTrigger;
            HorizontalSpeed = trigger.Damage.direction.x > 0.001f ? knockbackSpeed.x : -knockbackSpeed.x;
            VerticalSpeed = knockbackSpeed.y;
            CanDoubleJump = true;
            CanDash = true;

            gameObject.layer = LayerMask.NameToLayer("Invulnerable");
        }

        protected override void OnHitUpdate()
        {
            VerticalSpeed -= gravity * Time.deltaTime;
        }

        protected override void OnDeadEnter(FsmState prev, FsmTransition trans)
        {
            StartCoroutine(OnDeathEnterCorroutine());
        }

        public IEnumerator OnDeathEnterCorroutine()
        {
            animator.Play("Death", 0, 0);
            if (audioModule != null)
            {
                audioModule.PlayAudioRandom(AudioType.Dead);
                audioModule.PlayVoiceRandom(AudioType.DeadVoice);
            }
            foreach (var collider in bodyCollider)
            {
                collider.enabled = false;
            }
            HorizontalSpeed = 0f;
            VerticalSpeed = 0f;

            yield return null;
            while (!animator.IsCurrentStateEnd(0))
            {
                yield return null;
            }

            if (effectModule != null)
            {
                effectModule.Play(EffectType.Death, 0);
                while (effectModule.IsPlaying())
                {
                    yield return null;
                }
            }

            _deathEvent?.Invoke();
        }

        protected override void OnDeadExit(FsmState next, FsmTransition trans)
        {
            base.OnDeadExit(next, trans);
        }

        #endregion
    }


    public enum PlayerState
    {
        None = 0,
        Idle,
        Run,
        Focus,
        Spell,
        Attack,
        Dash,
        Jump,
        DoubleJump,
        Climb,
        Falling,
        Landing,
        Hit,
        Death,
    }

    public enum PlayerEffectState
    {
        None,
        AttackChargeInit,
        AttackChargeDone,
        AttackChargeLoop,
        SuperDashChargeInit,
        SuperDashChargeDone,
        FocusHeal,
        Invulnerable,
    }

    public class PlayerTransition : FsmTransition
    {
        public PlayerTransition(Func<bool> func, ITrigger trigger = null, bool transitAnyTime = true, int para = default)
        {
            conditionChecker = func;
            Trigger = trigger;
            this.transitAnyTime = transitAnyTime;
            IntPara = para;
        }
    }

    public class PlayerFsmState : FsmState
    {
        public PlayerFsmState(PlayerState state)
        {
            EnumState = state;
        }

    }

    public class EffectFsmState : FsmState
    {
        public EffectFsmState(PlayerEffectState state)
        {
            EnumState = state;
        }

    }


    public class PlayerFsmSystem : FsmSystem { }
    public class EffectFsmSystem : FsmSystem { }
}