using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    public class GhostZote : Zote
    {
        public float moveSpeed;
        public float verticalOffset;
        public float horizontalOffect;
        public float verticalNoise;
        public float changeDirTime;

        public float soulDrainRate;
        private Vector3 _originPosition;

        private float _soulDrainTime = 0f;
        private float _moveTime = 0f;
        public override void InitFsm()
        {
            base.InitFsm();
            characterFsmSystem
                .AddState(new ZoteFsmState(ZoteState.Idle))
                .AddState(new ZoteFsmState(ZoteState.Hit))
                .AddState(new ZoteFsmState(ZoteState.Dead))
                .AddState(new ZoteFsmState(ZoteState.None));
            var allState = characterFsmSystem.States;


            allState[ZoteState.Idle].RegisterProcess(OnIdleEnter, null, OnIdleUpdate)
                .AddTransition(new ZoteTransition(null, damageable.damageTrigger), allState[ZoteState.Hit]);

            allState[ZoteState.Hit].RegisterProcess(OnHitEnter,OnHitExit)
                .AddTransition(new ZoteTransition(() => !IsDead&&CurrentStateTime>0.05f), allState[ZoteState.Idle])
                .AddTransition(new ZoteTransition(() => IsDead), allState[ZoteState.Dead]);

            allState[ZoteState.Dead].RegisterProcess(OnDeadEnter)
                .AddTransition(new ZoteTransition(IsAllStopped), allState[ZoteState.None]);

            allState[ZoteState.None].RegisterProcess(OnNoneEnter);

            characterFsmSystem.SetDefaultState(ZoteState.Idle);
        }

        public override void Init()
        {
            base.Init();
            _originPosition = transform.position;
        }

        #region 状态机回调

        protected override void OnIdleUpdate()
        {
            _moveTime += Time.deltaTime;
            if (_moveTime>changeDirTime)
            {
                if (transform.position.x>_originPosition.x+horizontalOffect)
                {
                    HorizontalSpeed = -moveSpeed;
                }
                else if(transform.position.x < _originPosition.x - horizontalOffect)
                {
                    HorizontalSpeed = moveSpeed;
                }
                else
                {
                    HorizontalSpeed = Random.Range(0f, 1f) > 0.5f ? moveSpeed : -moveSpeed;
                }
                VerticalSpeed = transform.position.y > _originPosition.y ? -verticalNoise : verticalNoise;
                _moveTime = 0;
            }

            if (scanner.FindTarget)
            {
                if (!effectModule.IsPlaying())
                {
                    effectModule.Play(EffectType.SoulDrain,0);
                }
                ParticleSystem particle = effectModule.EffectDic[EffectType.SoulDrain][0].particleSystem;
                if (particle!=null)
                {
                    var shape = particle.shape;
                    var main = particle.main;
                    var particlePosition = particle.transform.position;
                    shape.position = (scanner.target.position - particlePosition)*0.8f;
                    main.startLifetime = shape.position.magnitude * 0.8f / Mathf.Abs(main.startSpeed.constantMax);

                    shape.rotation = new Vector3(0, 0,Vector3.SignedAngle(Vector3.up, shape.position,Vector3.forward));
                }
                
                Damageable damageable= scanner.target.GetComponent<Damageable>();
                if (damageable!=null)
                {
                    _soulDrainTime += Time.deltaTime;
                    if (_soulDrainTime>1/soulDrainRate)
                    {
                        damageable.currentStateInfo.mp.Data = damageable.currentStateInfo.mp.Data <= 0
                            ? 0
                            : damageable.currentStateInfo.mp.Data - 1;
                        _soulDrainTime = 0f;
                    }


                }
            }
            else
            {
                effectModule.Stop(EffectType.SoulDrain,0);
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

        protected override void OnHitExit(FsmState next, FsmTransition trans)
        {
            HorizontalSpeed = VerticalSpeed = 0f;
        }

        protected override void OnDeadEnter(FsmState prev, FsmTransition trans)
        {
            base.OnDeadEnter(prev, trans);
            effectModule.Stop(EffectType.SoulDrain,0);
            audioModule.StopPlay(AudioType.Idle);
        }

        #endregion
    }
}