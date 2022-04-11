using System;
using System.Collections.Generic;
using Demo2D.Frame;
using UnityEngine;

namespace Demo2D
{
    public enum ButtonState
    {
        None,
        Down,
        Press,
        Up,
    }

    public enum TriggerType
    {
        HoldUp = 1,
        Down =2,
        Up = 4, 
        HoldDown = 8,
        UpDown =16,
        HoldandRelease = 32,
    }

    //组合按键的切换条件
    public class CompositeTransit
    {
        //当前按键的触发条件
        public TriggerType triggerType;
        //允许当前按键状态变化，只要按键状态曾经满足条件，不管之后他是否改变，均认为前提条件成立;否则组合键前后两个按键必须同时成立才行
        public bool allowChange;
        //允许后一个按键触发时间和当前按键触发时间的最大间隔
        public float maxDeltaTime;
        //如果当前按键触发条件是HoldUP和HoldDown，需要设置按键保持时间
        public float holdTime;

        public CompositeTransit(TriggerType triggerType = TriggerType.Down, bool allowChange = true, float maxDeltaTime = 0f,float holdTime = 0f)
        {
            this.triggerType = triggerType;
            this.allowChange = allowChange;
            this.maxDeltaTime = maxDeltaTime;
            this.holdTime = holdTime;
        }
    }

    public class PlayerTrigger:ITrigger
    {
        public string name;

        public PlayerTrigger(string name)
        {
            this.name = name;
        }

        //按键触发事件的结果,内部类
        private class CheckRes
        {
            //是否触发了
            public bool triggered;
            //触发的时间
            public float time;

            public CheckRes():this(false,0f){}

            public CheckRes(bool triggered, float time)
            {
                this.triggered = triggered;
                this.time = time;
            }
        }

        //使用两个列表而不使用字典，是因为考虑到双击按键的组合键，会添加相同的key值
        private List<string> _inputButtonNames = new List<string>();
        private List<CompositeTransit> _transitions = new List<CompositeTransit>();

        //每个按键的状态
        private List<CheckRes> _results = new List<CheckRes>();

        //组合键触发的内置CD
        public float interval = 0.2f;
        //上一次触发的时间
        private float _lastTriggerTime;

        //触发的委托
        public Action<PlayerTrigger> onTriggered;

        public void AddButton(InputButton button, CompositeTransit transit)
        {
            //注册委托
            button.AddListener(CheckButton, transit.triggerType, transit.holdTime);
            //将按键和切换条件录入列表
            _inputButtonNames.Add(button.name);
            _transitions.Add(transit);
            //每个按键添加一个按键检查结果
            _results.Add(new CheckRes());
        }

        /// <summary>
        /// 按键每次触发dwon、up或者press的委托，根据预设的transitionl来判断是否按照预设的组合键条件触发
        /// </summary>
        /// <param name="inputButton">触发dwon、up或者press事件的按键</param>
        private void CheckButton(InputButton inputButton)
        {
            int index = FindLastUnchanged(inputButton.name);

            ButtonState state = inputButton.State;
            bool ready = inputButton.ready;
            CompositeTransit transit = _transitions[index];

            switch (transit.triggerType)
            {
                case TriggerType.HoldUp:
                    UpdateResults(index, state == ButtonState.None && ready, Time.unscaledTime);
                    break;
                case TriggerType.Down:
                    UpdateResults(index, state == ButtonState.Down, Time.unscaledTime);
                    break;
                case TriggerType.Up:
                    UpdateResults(index, state == ButtonState.Up, Time.unscaledTime);
                    break;
                case TriggerType.UpDown:
                    UpdateResults(index, state == ButtonState.Down || state ==ButtonState.Up, Time.unscaledTime);
                    break;
                case TriggerType.HoldDown:
                    UpdateResults(index, state == ButtonState.Press && true, Time.unscaledTime);
                    break;
                case TriggerType.HoldandRelease:
                    UpdateResults(index, state == ButtonState.Up && ready, Time.unscaledTime);
                    break;
                default:
                    Debug.LogError("未知的按键触发类型");
                    return ;
                
            }

        }

        /// <summary>
        /// 更新第index个按键的触发结果
        /// </summary>
        /// <param name="index">按键index</param>
        /// <param name="triggered">按键是否触发</param>
        /// <param name="time">按键触发的时间</param>
        private void UpdateResults(int index, bool triggered, float time = 0f)
        {
            
            //允许按键触发后松开
            if (_transitions[index].allowChange)
            {
                _results[index].triggered = _results[index].triggered || triggered;
            }
            else//不允许按键松开
            {
                _results[index].triggered = triggered;
            }

            if (triggered)//只有本次触发为true时，才跟新触发时间，否则直接返回
            {
                _results[index].time = time;
            }
            else
            {   
                return;
            }

            
            for (int i = 0; i < _results.Count; i++)
            {
                //没有触发,或者触发的时间顺序错误,或者触发时间超时
                if (!_results[i].triggered || 
                    (i > 0 && _results[i].time < _results[i - 1].time) ||
                    (i > 0 && _results[i].time - _results[i - 1].time >
                        _transitions[i - 1].maxDeltaTime)) 
                {
                    return;
                }

                //本次按键触发时间与上一个按键触发的时间差小于本次按键触发条件中的保持时间，说明本次按键开始计数的时间早于上一个按键的触发时间，属于触发时间顺序错误
                if (i>0&& _results[i].time - _results[i-1].time<_transitions[i].holdTime)
                {
                    return;
                }
            }

            if (Time.unscaledTime-_lastTriggerTime>=interval)
            {
                foreach (var res in _results)//触发一次组合键后需要重置结果列表
                {
                    res.time = 0;
                    res.triggered = false;
                }
                Trigger();
            }
        }

        /// <summary>
        /// 找到组合按键队列中名字为name的按键里最晚触发的index。复杂的组合键里面，同一个按键可能会出现多次，因此，需要从多个相同的按键中得到最晚更新的按键的index
        /// </summary>
        /// <param name="name">按键名称</param>
        /// <returns>按键的index</returns>
        private int FindLastUnchanged(string name)
        {
            int index = 0;
            float time = Mathf.Infinity;
            float temp = 0;
            for (int i = 0; i < _inputButtonNames.Count; i++)
            {
                if (_inputButtonNames[i] == name)
                {
                    temp = _results[i].time;
                    if (temp < time)
                    {
                        time = temp;
                        index = i;
                    }
                }
            }
            return index;
        }

        public PlayerTrigger Content => this;

        public void AddListener(Action<ITrigger > listener)
        {
            onTriggered += listener;

        }

        public void RemoveListener(Action<ITrigger> listener)
        {
            onTriggered -= listener;
        }

        public void Trigger()
        {
            onTriggered?.Invoke(this);
        }
    }

    //输入按键
    public class InputButton
    {
        //按键的名字
        public string name;

        //对应的KeyCode，为了实现自定义按键，因此要使用getkey来代替getbutton
        public KeyCode keyCode;

        //todo:这里我没有找到手柄的RT和LT的获取方法，只能通过Input Manager 来获取对应的值，有待解决
        //对应的Input Manager里的axis的名字
        public string axisName;

        private ButtonState _state;
        //按键状态，每次有效触发会调用委托
        public ButtonState State
        {
            get => _state;
            set
            {
                _state = value;
                onButtonTriggerd?.Invoke(this);
            }
        }

        //按键需要在update中检查的触发模式
        public int CheckMask { get; set; }

        //长按或者停按的保持时间
        public float holdTime;
        //长按时间阈值，长按时间超过阈值,按键变为可击发状态
        public float holdThreshold;

        //私有变量，标记是否为可击发状态
        public bool ready = false;

        public Action<InputButton> onButtonTriggerd;

        private void Set(string name, float holdThreshold = Mathf.Infinity, float holdTime = 0f,
            ButtonState state = ButtonState.None, bool ready = false)
        {
            this.name = name;
            this.holdThreshold = holdThreshold;

            this._state = state;
            this.holdTime = holdTime;
            this.ready = ready;
        }

        public InputButton(string name, float threshold = Mathf.Infinity)
        {
            Set(name,threshold);
        }

        public void Reset()
        {
            Set(default);
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="listener">按键触发的委托</param>
        /// <param name="type">监听的按键触发类型</param>
        public void AddListener(Action<InputButton> listener,TriggerType type, float holdTime = 0f)
        {
            onButtonTriggerd += listener;
            CheckMask |= (int) type;
            holdThreshold = holdTime;
        }

        public virtual void CheckButtonState()
        {
            if (IncludeTriggerType(TriggerType.Down) && CheckButtonDown())//按下动作
            {
                //Debug.Log("button down: "+name);
                State = ButtonState.Down;
                holdTime = 0f;
            }
            else if (IncludeTriggerType(TriggerType.HoldDown) && CheckButtonPress())//按住动作
            {
                //Debug.Log("button press: " + name);

                State = ButtonState.Press;
                holdTime += Time.unscaledDeltaTime;
                if (holdTime > holdThreshold)//长按时间超过阈值
                {
                    ready = true;
                }

            }
            else if (IncludeTriggerType(TriggerType.Up) && CheckButtonUp())//松开动作
            {
                //Debug.Log("button up: " + name);

                State = ButtonState.Up;
                if (ready)//击发
                {
                    ready = false;
                }
                holdTime = 0f;

            }
            else 
            {
                State = ButtonState.None;
                holdTime += Time.unscaledDeltaTime;
                if (holdTime > holdThreshold)//长按时间超过阈值
                {
                    ready = true;
                    holdTime = 0f;
                }
            }
        }

        public bool IncludeTriggerType(TriggerType type)
        {
            return (CheckMask & (int)type) != 0;
        }

        public virtual bool CheckButtonPress()
        {
            return Input.GetButton(name);
        }
        public virtual bool CheckButtonDown()
        {
            return Input.GetButtonDown(name);
        }

        public virtual bool CheckButtonUp()
        {
            return Input.GetButtonUp(name);
        }


    }

    public class InputAxis:InputButton
    {

        private float _value;
        public float Value
        {
            get => _value;
            set => _value = value;
        }

        public override void CheckButtonState()
        {
            float temp = Input.GetAxis(axisName);

            if (IncludeTriggerType(TriggerType.Down) && Value < temp && temp > 0.9f)//按下动作
            {
                //Debug.Log("button down: " + name);
                State = ButtonState.Down;
                holdTime = 0f;
            }
            else if (IncludeTriggerType(TriggerType.HoldDown) && temp >= 0.9f)//按住动作
            {
                //Debug.Log("button press: " + name);

                State = ButtonState.Press;
                holdTime += Time.unscaledDeltaTime;
                if (holdTime > holdThreshold)//长按时间超过阈值
                {
                    ready = true;
                }

            }
            else if (IncludeTriggerType(TriggerType.Up) && Value > temp && temp < 0.1f)//松开动作
            {
                //Debug.Log("button up: " + name);

                State = ButtonState.Up;
                if (ready)//击发
                {
                    ready = false;
                }
                holdTime = 0f;

            }
            else
            {
                State = ButtonState.None;
                holdTime += Time.unscaledDeltaTime;
                if (holdTime > holdThreshold)//长按时间超过阈值
                {
                    ready = true;
                    holdTime = 0f;
                }
            }

            Value = temp;
        }

        public InputAxis(string name, string axisName,float threshold = Mathf.Infinity) : base(name, threshold)
        {
            this.axisName = axisName;
        }

    }


    public class PlayerInput : MonoBehaviour
    {
        private static PlayerInput _instance;

        public static PlayerInput Instance => _instance;

        public bool BlockInput { get; set; } = false;

        //收录全部的按键，key为按键名，value为按键信息
        private Dictionary<string, InputButton> _allButtonDic = new Dictionary<string, InputButton>();
        private Dictionary<string, PlayerTrigger> _allPlayerTriggers = new Dictionary<string, PlayerTrigger>();


        public PlayerTrigger GetPlayerTrigger(string name)
        {
            if (_allPlayerTriggers.TryGetValue(name, out var cmkey) && cmkey != null)
            {
                return cmkey;
            }

            Debug.Log("PlayerInput找不到组合按键：" + name);
            return null;
        }

        public InputButton GetInputButton(string name)
        {
            if (_allButtonDic.TryGetValue(name, out var button) && button != null)
            {
                return button;
            }
            Debug.Log("PlayerInput找不到按键：" + name);
            return null;
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }

            if (Instance !=this)
            {
                Destroy(gameObject);
            }
            


            _allButtonDic.Add("Attack", new InputButton("Attack"));
            _allButtonDic.Add("Jump", new InputButton("Jump"));
            _allButtonDic.Add("Focus", new InputButton("Focus"));
            _allButtonDic.Add("Dash", new InputButton("Dash"));
            _allButtonDic.Add("SuperDash", new InputButton("SuperDash"));
            _allButtonDic.Add("DashJoystick",new InputAxis("DashJoystick","RT"));
            _allButtonDic.Add("SuperDashJoystick", new InputAxis("DashJoystick", "LT"));

            PlayerTrigger attack = new PlayerTrigger("Attack");
            PlayerTrigger dash = new PlayerTrigger("Dash");
            PlayerTrigger dashJoystickKey = new PlayerTrigger("DashJoystick");
            PlayerTrigger jump = new PlayerTrigger("Jump");
            PlayerTrigger jumpAbort = new PlayerTrigger("JumpAbort");
            PlayerTrigger focus = new PlayerTrigger("Focus");
            PlayerTrigger spell = new PlayerTrigger("Spell");
            PlayerTrigger superDashCharge = new PlayerTrigger("SuperDashCharge");
            PlayerTrigger superDashRelease = new PlayerTrigger("SuperDashRelease");
            PlayerTrigger attackCharge = new PlayerTrigger("AttackCharge");
            PlayerTrigger attackRelease = new PlayerTrigger("AttackRelease");

            attack.AddButton(_allButtonDic["Attack"], new CompositeTransit());
            dash.AddButton(_allButtonDic["Dash"], new CompositeTransit());
            dashJoystickKey.AddButton(_allButtonDic["DashJoystick"], new CompositeTransit());
            jump.AddButton(_allButtonDic["Jump"], new CompositeTransit());
            focus.AddButton(_allButtonDic["Focus"], new CompositeTransit(TriggerType.HoldDown,false));
            spell.AddButton(_allButtonDic["Focus"], new CompositeTransit(TriggerType.Up));
            superDashCharge.AddButton(_allButtonDic["SuperDashJoystick"], new CompositeTransit(TriggerType.HoldDown,false));
            superDashRelease.AddButton(_allButtonDic["SuperDashJoystick"], new CompositeTransit(TriggerType.Up));
            jumpAbort.AddButton(_allButtonDic["Jump"],new CompositeTransit(TriggerType.Up));
            attackCharge.AddButton(_allButtonDic["Attack"],new CompositeTransit(TriggerType.HoldDown,holdTime:0.2f));
            attackRelease.AddButton(_allButtonDic["Attack"],new CompositeTransit(TriggerType.Up));

            _allPlayerTriggers.Add("Attack",attack);
            _allPlayerTriggers.Add("Dash",dash);
            _allPlayerTriggers.Add("Jump",jump);
            _allPlayerTriggers.Add("JumpAbort",jumpAbort);
            _allPlayerTriggers.Add("Focus",focus);
            _allPlayerTriggers.Add("Spell",spell);
            _allPlayerTriggers.Add("SuperDashCharge",superDashCharge);
            _allPlayerTriggers.Add("SuperDashRelease",superDashRelease);
            _allPlayerTriggers.Add("AttackCharge",attackCharge);
            _allPlayerTriggers.Add("AttackRelease", attackRelease);
            _allPlayerTriggers.Add("DashJoystick", dashJoystickKey);
        }

        public float MoveInput { get; private set; }

        public float CameraInput { get; private set; }

        public float LookInput { get; private set; }



        void Update()
        {
            if (!BlockInput)
            {
                foreach (var name in _allButtonDic.Keys)
                {
                    _allButtonDic[name].CheckButtonState();
                }

                MoveInput = Input.GetAxis("Horizontal");
                LookInput = Input.GetAxis("Vertical");
            }

        }

    }
}