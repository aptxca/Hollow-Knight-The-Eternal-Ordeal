using System;
using Demo2D.Frame;
using UnityEngine;

namespace Demo2D
{
    public enum ZoteState
    {
        None,
        Entrance,
        Idle,
        Attack,
        Jump,
        Falling,
        Landing,
        GetUp,
        Fell,
        Dead,
        Hit,
        Any,
        PathFinding,
        Leave
    }

    public class ZoteTransition : FsmTransition
    {
        public ZoteTransition(Func<bool> func = null, ITrigger trigger = null,
            bool transitAnyTime = true, int para = default)
        {
            conditionChecker = func;
            Trigger = trigger;
            this.transitAnyTime = transitAnyTime;
            IntPara = para;
        }

    }

    public class ZoteFsmState : FsmState
    {
        public ZoteFsmState(ZoteState state)
        {
            EnumState = state;
        }
    }

    public class ZoteFsmSystem : FsmSystem
    {

    }

    public class Zote : EnemyController
    {
        
        void Awake()
        {
            Init();
            InitFsm();
        }

        void OnEnable()
        {
            if (characterFsmSystem.CurrentState != null)
            {
                characterFsmSystem.ReStart();
            }

            foreach (var collider in bodyCollider)
            {
                collider.enabled = true;
            }
        }


        void Update()
        {
            characterFsmSystem.OnUpdate();
        }

        void OnDisable()
        {
            ResetStatus();
        }

        public virtual void InitFsm()
        {
            characterFsmSystem = new ZoteFsmSystem();
        }

        public override void Init()
        {
            base.Init();
            bodyCollider = GetComponents<Collider2D>();
        }

        #region 状态机相关回调

        protected override void OnNoneEnter(FsmState prev, FsmTransition trans)
        {
            HorizontalSpeed = 0f;
            VerticalSpeed = 0f;
            GameObjectManager.Instance.Recycle(gameObject);
        }

        #endregion
    }
}
