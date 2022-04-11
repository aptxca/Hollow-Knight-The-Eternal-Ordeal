using System;
using System.Collections.Generic;
using UnityEngine;

/*
 * 有限状态机相关类
 */


/*
 *状态类：FSMState
 *状态的切换条件类：FSMTransition
 *状态管理类：FSMSystem
 *
 *
 * 
 */

namespace Demo2D.Frame
{
    //触发事件接口
    public interface ITrigger
    {
        //T Content { get;}

        void AddListener(Action<ITrigger> listener);

        void RemoveListener(Action<ITrigger> listener);

        void Trigger();
    }


    //状态转换条件
    public abstract class FsmTransition
    {
        //public object Para { get; set; }

        public int IntPara { get; set; }
        public float FloatPara { get; set; }
        public string StringPara { get; set; }
        public object ObjPara { get; set; }

        //public bool checkEveryUpdate;
        public bool transitAnyTime;
        public ITrigger Trigger { get; protected set; } = null;

        //public abstract ITrigger GetTrigger() where T : ITrigger;
        //public abstract ITrigger GetTrigger();

        public void AddCondition(Func<bool> condition)
        {
            conditionChecker += condition;
        }

        protected Func<bool> conditionChecker;

        public bool CheckCondition(bool truefornull)
        {
            if (conditionChecker == null)
            {
                return truefornull;
            }

            return conditionChecker.Invoke();
        }


    }


    //状态
    public abstract class FsmState
    {
        public Enum EnumState { get; protected set; }

        public int IntPara { get; set; }
        public float FloatPara { get; set; }
        public string StringPara { get; set; }
        public object ObjPara { get; set; }

        public float Time { get; protected set; }
        public bool EndOfLoop { get; protected set; }

        //收录本状态到别的状态的转换
        public Dictionary<FsmTransition, FsmState> map = new Dictionary<FsmTransition, FsmState>();

        //收录所有基于触发式事件的转换条件
        private List<FsmTransition> _triggerBasedTransitions = null;
        protected List<FsmTransition> TriggerBasedTransitions
        {
            get
            {
                if (_triggerBasedTransitions == null)
                {
                    _triggerBasedTransitions = new List<FsmTransition>();
                    foreach (var trans in map.Keys)
                    {
                        ITrigger temp = trans.Trigger;
                        if (temp != null)
                        {
                            _triggerBasedTransitions.Add(trans);
                        }
                    }
                }

                return _triggerBasedTransitions;
            }
        }


        //收录所有的ITrigger，ITrigger个数必定不超过事件驱动类型的FSMTransition数量，因为有些FSMTransition的ITrigger是相同的，这些FSMTransition只有自个的conditionChecker不同
        private HashSet<ITrigger> _relatedTriggers = null;
        protected HashSet<ITrigger> RelatedTrigger
        {
            get
            {
                if (_relatedTriggers ==null)
                {
                    _relatedTriggers = new HashSet<ITrigger>();
                    foreach (var trans in TriggerBasedTransitions)
                    {
                        ITrigger temp = trans.Trigger;
                        if (!_relatedTriggers.Contains(temp))
                        {
                            _relatedTriggers.Add(temp);
                        }   
                    }
                }

                return _relatedTriggers;
            }
        } 

        /// <summary>
        /// 添加到下一状态的转换
        /// </summary>
        /// <param name="transition">转换条件</param>
        /// <param name="next">下一状态</param>
        /// <returns></returns>
        public virtual FsmState AddTransition(FsmTransition trans, FsmState next)
        {
            if (trans == default)
            {
                Debug.Log("添加的转换条件为空");
                return this;
            }

            if (map.ContainsKey(trans))
            {
                Debug.Log("已存在转换条件");
                return this;
            }

            map.Add(trans, next);
            return this;

        }

        /// <summary>
        /// 注册本状态下的委托
        /// </summary>
        /// <param name="enter">进入本状态时的委托</param>
        /// <param name="exit">退出本状态时的委托</param>
        /// <param name="update">本状态运行时的委托，每个update调用</param>
        /// <returns></returns>
        public virtual FsmState RegisterProcess(Action<FsmState, FsmTransition> enter = null,
            Action<FsmState, FsmTransition> exit =null, Action update= null, Action loopEndCallBack = null)
        {
            enterProcess += enter;
            exitProcess += exit;
            updateProcess += update;
            this.loopEndCallBack += loopEndCallBack;
            return this;
        }

        public Action<FsmState, FsmTransition> enterProcess;
        public Action<FsmState, FsmTransition> exitProcess;
        public Action updateProcess;

        public Action loopEndCallBack;

        protected FsmTransition loopEndTransition = null;

        /// <summary>
        /// 进入本状态的方法
        /// </summary>
        /// <param name="prev">上一个状态</param>
        /// <param name="transition">自上一个状态过来的转换条件</param>
        public virtual void OnEnter(FsmState prev, FsmTransition trans)
        {
            Time = 0f;
            EndOfLoop = false;
            foreach (var trigger in RelatedTrigger)
            {
                trigger.AddListener(OnInputTrigger);
            }
            enterProcess?.Invoke(prev, trans);
        }

        /// <summary>
        /// 退出本状态时的方法
        /// </summary>
        /// <param name="next">下一个状态</param>
        /// <param name="transition">转到下一个状态的条件</param>
        public virtual void OnExit(FsmState next, FsmTransition trans)
        {
            foreach (var trigger in RelatedTrigger)
            {
                trigger.RemoveListener(OnInputTrigger);
            }
            exitProcess?.Invoke(next, trans);
        }

        public void ClearAllTriggers()
        {
            foreach (var trigger in RelatedTrigger)
            {
                trigger.RemoveListener(OnInputTrigger);
            }
        }


        /// <summary>
        /// 每个update调用，用于检查转换条件是否成立
        /// </summary>
        public virtual void OnUpdate()
        {
            Time += UnityEngine.Time.deltaTime;
            updateProcess?.Invoke();
            if (EndOfLoop)
            {
                //在update中有达成条件的transition,立即切换状态
                if (loopEndTransition != null)
                {
                    //Debug.Log("loopendtransit 1 ");

                    MakeTransition(loopEndTransition);
                    loopEndTransition = null;
                    return;
                }
                foreach (var trans in map.Keys)
                {

                    //只检查每轮循环后刷新结果的transition
                    if (!TriggerBasedTransitions.Contains(trans))
                    {
                        //由于已经在循环结束的callback中，不论是需要立即切换的transition还是需要等循环结束时切换的transition，只要达成条件，均直接切换
                        if (trans.CheckCondition(false))
                        {
                            //Debug.Log("loopendtransit 2 ");
                            MakeTransition(trans);
                            return;
                        }
                    }
                }
            }


            foreach (var trans in map.Keys)
            {
                //只检查非触发式transition
                if (!TriggerBasedTransitions.Contains(trans))
                {
                    //只检查需要每帧刷新结果的transition
                    //if (trans.checkEveryUpdate)
                    if (true)
                    {
                        if (trans.CheckCondition(false))
                        {
                            //符合条件，并且可随时切换
                            if (trans.transitAnyTime && loopEndTransition==null)
                            {
                                MakeTransition(trans);
                            }
                            //符合条件，但要求状态循环结束时切换
                            else
                            {
                                loopEndTransition = trans;
                            }
                            return;
                        }
                    }
                }

            }


        }

        //状态每个循环结束时手动调用
        public virtual void OnStateLoopEnd()
        {
            EndOfLoop = true;

            //Debug.Log("loopend: " + this.state);
            loopEndCallBack?.Invoke();

        }


        public void OnInputTrigger(ITrigger trigger)
        {
            foreach (var trans in _triggerBasedTransitions)
            {
                if (trans.Trigger == trigger)
                {
                    if (trans.CheckCondition(true))
                    {
                        //符合条件，并且可随时切换
                        if (trans.transitAnyTime)
                        {
                            //Debug.Log("trigger check: " + map[trans].EnumState);
                            MakeTransition(trans);
                        }
                        //符合条件，但要求状态循环结束时切换
                        else
                        {
                            //Debug.Log("trigger check: " + map[trans].EnumState);
                            loopEndTransition = trans;
                        }
                        return;
                    }
                }
            }

        }

        /// <summary>
        /// 执行状态切换
        /// </summary>
        /// <param name="transition">成立的转换条件</param>
        public void MakeTransition(FsmTransition trans)
        {
            onTransition?.Invoke(map[trans], trans);
        }

        public Action<FsmState, FsmTransition> onTransition;

    }

    
    //状态管理类
    public class FsmSystem
    {
        //收录全部的状态,key为状态名称， value为状态
        private Dictionary<Enum, FsmState> _states = new Dictionary<Enum, FsmState>();
        //当前状态
        private FsmState _currentState;
        public FsmState DefoultState { get; protected set; }

        public bool isTransiting = false;

        public Dictionary<Enum, FsmState> States => _states;
        public FsmState CurrentState => _currentState;
        //状态切换事件委托，向外提供监听接口
        private Action<Enum> _stateChangeEvent;
        //任意状态下的相同操作
        public Action onAnyStateUpdate;
        public FsmState AnyState { get; protected set; }
        //添加状态切换监听委托
        public void AddStateChangeEventListener(Action<Enum> onStateChange)
        {
            _stateChangeEvent += onStateChange;
        }
        public void OnUpdate()
        {
            if (!isTransiting)
            {
                _currentState.OnUpdate();

                AnyState?.OnUpdate();
            }
        }

        public void OnStart()
        {
            _currentState?.OnEnter(null, null);

            AnyState?.OnEnter(null,null);
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public  FsmSystem AddState(FsmState state)
        {
            if (state == default)
            {
                Debug.Log("空的状态");
                return this;
            }

            if (_states.ContainsValue(state))
            {
                Debug.Log("已存在状态");
                return this;
            }

            if (_currentState == default)
            {
                _currentState = state;
            }

            state.onTransition += OnTransition;
            _states.Add(state.EnumState, state);
            return this;
        }

        public FsmSystem AddAnyState(FsmState state)
        {
            if (state == default)
            {
                Debug.Log("空的状态");
                return this;
            }

            if (_states.ContainsValue(state))
            {
                Debug.Log("已存在状态");
                return this;
            }

            state.onTransition += OnTransition;
            AnyState = state;
            return this;
        }
        /// <summary>
        /// 删除状态
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool RemoveState(FsmState state)
        {
            if (state == default)
            {
                Debug.Log("空的状态");
                return false;
            }

            if (!_states.ContainsValue(state))
            {
                Debug.Log("不存在状态" + state.EnumState + ", 删除无效");
                return false;
            }

            _states[state.EnumState].onTransition -= OnTransition;
            _states.Remove(state.EnumState);
            return true;
        }

        /// <summary>
        /// 状态切换回调
        /// </summary>
        /// <param name="next">下一个状态</param>
        /// <param name="transition">转换条件</param>
        protected void OnTransition(FsmState next, FsmTransition trans)
        {
            //Debug.Log("发生转换：" + _currentState.EnumState + " -> " + next.EnumState);
            _currentState.OnExit(next, trans);
            FsmState temp = _currentState;
            if (!_states.ContainsValue(next))
            {
                Debug.Log("有限状态机里没有" + next.EnumState + "这一状态");
                return;
            }
            _currentState = next;
            _currentState.OnEnter(temp, trans);
            _stateChangeEvent?.Invoke(_currentState.EnumState);
        }
        /// <summary>
        /// 设置默认状态
        /// </summary>
        /// <param name="state">状态</param>
        public void SetDefaultState(FsmState state)
        {
            if (_states.ContainsValue(state))
            {
                DefoultState = state;
                _currentState = state;
            }
        }

        public void RegisterAnyStateUpdate(Action onAnyStateUpdate)
        {
            this.onAnyStateUpdate += onAnyStateUpdate;
        }
        public void UnRegisterAnyStateUpdate(Action onAnyStateUpdate)
        {
            this.onAnyStateUpdate -= onAnyStateUpdate;
        }

        public void RegisterAnyStateTrigger(ITrigger trigger,Action<ITrigger> callBack)
        {
            trigger.AddListener(callBack);
        }

        public void UnRegisterAnyStateTrigger(ITrigger trigger, Action<ITrigger> callBack)
        {
            trigger.RemoveListener(callBack);
        }

        public void SetDefaultState(Enum enumState)
        {
            if (_states.TryGetValue(enumState, out var fsmState) && fsmState != null)
            {
                DefoultState = fsmState;
                _currentState = fsmState;
            }
            else
            {
                Debug.Log("找不到状态：" + enumState);
            }
        }

        public void ReStart()
        {
            if (DefoultState==null)
            {
                Debug.LogError("有限状态机未设置默认状态，使用SetDefaultState方法设置默认状态");
            }
            _currentState = DefoultState;
            foreach (var state in States.Values)
            {
                state.ClearAllTriggers();
            }
            OnStart();
        }
    }
}