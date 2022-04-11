using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Demo2D
{
    [DefaultExecutionOrder(1100)]
    public class HealthBar : MonoBehaviour
    {
        public string heathOrbResPath;

        private CharacterStateInfo _currentState;
        private CharacterStateInfo _maxState;

        public  List<HealthOrb> heathOrbs;
        //指向最后一颗未损坏的小球
        public int orbIndex;

        public void Init(Damageable damageble)
        {
            if (damageble ==null)
            {
                return;
            }

            _currentState = damageble.currentStateInfo;
            _maxState = damageble.maxStateInfo;
            

            if (_currentState == null || _maxState == null)
            {
                Debug.Log("找不到角色的状态信息");
                return;
            }

            heathOrbs ??= new List<HealthOrb>();

            int maxHealth = _maxState.hp.Data;
            if (heathOrbs.Count<maxHealth)
            {
                for (int i = heathOrbs.Count; i < maxHealth; i++)
                {
                    GameObject orb = GameObjectManager.Instance.InstantiateGameObject(heathOrbResPath);
                    var orbRectTrans = orb.transform as RectTransform;
                    if (orbRectTrans == null)
                    {
                        Debug.Log("无法转换成rectTrans,血条加载失败");
                        return;
                    }
                    orbRectTrans.SetParent(transform, false);
                    orbRectTrans.anchoredPosition = Vector2.right * 70f * i;
                    heathOrbs.Add(orb.GetComponent<HealthOrb>());
                }
            }
            else
            {
                for (int i = heathOrbs.Count-1; i >= maxHealth; i--)
                {
                    GameObjectManager.Instance.Recycle(heathOrbs[i].gameObject);
                }
            }

            orbIndex = maxHealth - 1;
            _currentState.hp.onValueChanged += OnHealthChange;
            _currentState.mp.onValueChanged += OnSoulChange;
        }

        private void OnSoulChange(int obj)
        {
            
        }

        private void OnHealthChange(int hp)
        {
            int delta = hp - orbIndex - 1;
            if (delta>0)//加血
            {
                while (delta > 0 && orbIndex < heathOrbs.Count - 1)
                {
                    orbIndex++;
                    delta--;
                    heathOrbs[orbIndex].SetState(true);
                }
            }
            else//减血
            {
                while (delta<0&&orbIndex>=0)
                {

                    heathOrbs[orbIndex].SetState(false);
                    orbIndex--;
                    delta++;
                }   
            }
        }
    }
}