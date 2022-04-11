using System;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Explode,
    Death,
    SoulDrain,
    LandDust,
}

[Serializable]
public class Effect
{
    public EffectType type;
    public ParticleSystem particleSystem;
}

public class MonoEffectModule : MonoBehaviour
{
    private Dictionary<EffectType, List<Effect>> _effectDic = new Dictionary<EffectType, List<Effect>>();
    private List<Effect> _activeEffects = new List<Effect>();
    public List<Effect> effects;
    public Dictionary<EffectType, List<Effect>> EffectDic => _effectDic;
    public bool IsPlaying()
    {
        if (_activeEffects.Count>0)
        {
            foreach (var effect in _activeEffects)
            {
                if (effect.particleSystem.isPlaying)
                {
                    return true;
                }
            }
        }
        _activeEffects.Clear();
        return false;
    }

    private void Init()
    {
        foreach (var effect in effects)
        {
            if (!_effectDic.ContainsKey(effect.type))
            {
                _effectDic.Add(effect.type,new List<Effect>());
            }
            _effectDic[effect.type].Add(effect);
        }
    }

    public void Play(EffectType type, int index)
    {
        SetEffect(type,index,true);
    }

    public void Stop(EffectType type, int index)
    {
        SetEffect(type, index, false);
    }

    private void SetEffect(EffectType type, int index,bool play)
    {
        if (index < 0)
        {
            Debug.Log("effect index不能为负数：" + index);
            return;
        }

        if (!_effectDic.TryGetValue(type, out var effectList))
        {
            //Debug.Log("找不到类型" + type + "的effect");
            return;
        }

        if (index >= effectList.Count)
        {
            Debug.Log("找不到第" + index + "个类型为" + type + "的Effect，默认操作一个effect");
            if (play)
            {
                effectList[0].particleSystem.Play();
            }
            else
            {
                effectList[0].particleSystem.Stop();
            }
            return;
        }

        if (play)
        {
            effectList[index].particleSystem.Play();
            if (!_activeEffects.Contains(effectList[index]))
            {
                _activeEffects.Add(effectList[index]);
            }
        }
        else
        {
            effectList[index].particleSystem.Stop();
            if (_activeEffects.Contains(effectList[index]))
            {
                _activeEffects.Remove(effectList[index]);
            }
        }

    }

    public void Awake()
    {
        Init();
    }
}
