using System;
using System.Collections;
using System.Collections.Generic;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;
using UnityEngine.UI;

namespace Demo2D
{
    public class UIManager : MonoSingleton<UIManager>
    {
        public List<string> menuResPaths;
        public GameState uiState;
        public Stack<MenuController> activeMenus = new Stack<MenuController>();

        //本项目中每种菜单是唯一的，不存在多开的情况
        private Dictionary<MenuType, MenuController> _allMenuDic = new Dictionary<MenuType, MenuController>();
        

        public void Init()
        {
            GameObjectResource objRes;
            RectTransform rectTrans;
            MenuController controller;
            if (menuResPaths==null)
            {
                return;
            }
            foreach (var path in menuResPaths)
            {
                objRes = GameObjectManager.Instance.InstantiateGameObjectRes(path);
                if (objRes.objClone != null)
                {
                    rectTrans = objRes.objClone.transform as RectTransform;
                    if (rectTrans == null)
                    {
                        Debug.Log("对象：" + objRes.path + "transform组件无法转换成rectTransform组件，跳过");
                        continue;
                    }

                    controller = objRes.objClone.GetComponent<MenuController>();
                    if (controller == null)
                    {
                        Debug.Log("找不到MenuController组件，跳过");
                        continue;
                    }

                    rectTrans.SetParent(transform, false);
                    controller.canvasGroup.alpha = 0;
                    controller.canvasGroup.interactable = false;
                    controller.canvasGroup.blocksRaycasts = false;
                    if (!_allMenuDic.ContainsKey(controller.type))
                    {
                        _allMenuDic.Add(controller.type, controller);
                    }

                }
            }
        }

        public GameObject CreateUIObject(string resPath, Vector2 position)
        {

            GameObjectResource objRes = GameObjectManager.Instance.InstantiateGameObjectRes(resPath);
            if (objRes != null && objRes.objClone != null)
            {
                RectTransform rectTrans = objRes.objClone.transform as RectTransform;
                if (rectTrans != null)
                {
                    rectTrans.SetParent(transform, false);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, position,
                        null, out var uiPoint);
                    rectTrans.anchoredPosition = uiPoint;
                    return objRes.objClone;
                }
            }

            return null;
        }

        public void SetUIObjectPosition(RectTransform rectTrans, Vector2 screenPoint)
        {
            if (rectTrans != null)
            {
                //rectTrans.SetParent(transform, false);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, screenPoint,
                     null, out var uiPoint);
                rectTrans.anchoredPosition = uiPoint;
            }
        }

        public void RemoveUIObject(GameObject obj, bool detach)
        {
            GameObjectManager.Instance.Recycle(obj, detach);
        }

        public void Pause(Action callback)
        {
            OpenMenu(MenuType.PauseMenu, isParentVisiable: true);
            callback?.Invoke();
        }

        public void Resume(Action callback)
        {
            StartCoroutine(ResumeGameCoroutine(callback));
        }

        public IEnumerator ResumeGameCoroutine(Action callback)
        {

            if (activeMenus.Peek().type!=MenuType.Hud)
            {
                yield return StartCoroutine( CloseMenuCoroutine(activeMenus.Peek(), false));
            }

            while (activeMenus.Count > 0&&activeMenus.Peek().type != MenuType.Hud)
            {
                yield return StartCoroutine(CloseMenuCoroutine(activeMenus.Peek(), true));
            }

            if (activeMenus.Count>0&&activeMenus.Peek().type==MenuType.Hud)
            {
                uiState = GameState.Gaming;
            }

            callback?.Invoke();
        }

        public void BackToParentMenu(MenuController menu)
        {
            CloseMenu(menu,false,()=> OpenMenu(activeMenus.Peek().type));
        }

        public void OpenMenu(MenuType type, bool keepParentAlive = true, bool isParentVisiable = false,
            bool blockParent = true)
        {
            StartCoroutine(OpenMenuCoroutine(type, keepParentAlive, isParentVisiable, blockParent));
        }

        public void CloseMenu(MenuController menu, bool immediately = false, Action onClosed = null)
        {
            StartCoroutine(CloseMenuCoroutine(menu, immediately, onClosed));
        }

        public void CloseAllMenu(Action callback)
        {
            StartCoroutine(CloseAllMenuCoroutine(callback));
        }



        public void ApplyCmd(UiButtonCmd cmd)
        {
            switch (cmd)
            {
                case UiButtonCmd.Start:
                case UiButtonCmd.Resume:
                case UiButtonCmd.BackToMainMenu:
                case UiButtonCmd.ExitGame:
                    SendUIRequest(cmd);
                    break;
            }

        }

        public void SendUIRequest(UiButtonCmd cmd)
        {
            GameManager.Instance.processUIRequest(cmd);
        }

        public void RegisterPlayerStatus(Damageable damageable)
        {
            if (!_allMenuDic.TryGetValue(MenuType.Hud, out var menu) || menu == null)
            {
                Debug.Log("没找到hud");
                return;
            }

            Hud hud = menu as Hud;
            if (hud ==null)
            {
                Debug.Log("没找到hud");
                return;
            }
            hud.RegisterPlayerStatus(damageable);
        }

        public void RegisterScore(ReactiveProperty<int> score)
        {
            if (!_allMenuDic.TryGetValue(MenuType.Hud, out var menu) || menu == null)
            {
                Debug.Log("没找到hud");
                return;
            }

            Hud hud = menu as Hud;
            if (hud == null)
            {
                Debug.Log("没找到hud");
                return;
            }

            if (score!=null)
            {
                score.onValueChanged += hud.OnScoreChange;
            }
        }

        protected IEnumerator CloseMenuCoroutine(MenuController menu, bool immediately = false, Action callback = null)
        {
            if (immediately)
            {
                menu.CloseImmediately();
            }
            else
            {
                yield return menu.Close();
            }
            activeMenus.Pop();
            callback?.Invoke();
        }

        private IEnumerator CloseAllMenuCoroutine(Action callback)
        {
            if (activeMenus.Count == 0)
            {
                callback?.Invoke();
                yield break;
            }

            if (activeMenus.Peek().type != MenuType.Hud)
            {
                yield return StartCoroutine(CloseMenuCoroutine(activeMenus.Peek(), false));
            }

            while (activeMenus.Count > 0 )
            {
                yield return StartCoroutine(CloseMenuCoroutine(activeMenus.Peek(), true));
            }

            callback?.Invoke();
        }

        public IEnumerator OpenMenuCoroutine(MenuType type, bool keepParentAlive = true, bool isParentVisiable = false, bool blockParent = true)
        {
            if (!_allMenuDic.TryGetValue(type, out var menu) || menu == null)
            {
                Debug.Log("没找到menu: " + type);
                //return;
                yield break;
            }

            if (activeMenus.Count == 0)
            {
                menu.Open();
                activeMenus.Push(menu);
                //return;
                yield break;
            }

            if (!keepParentAlive)
            {
                yield return activeMenus.Peek().Close();
                activeMenus.Pop();
            }
            else
            {
                if (!isParentVisiable)
                {
                    yield return activeMenus.Peek().Close();
                    activeMenus.Peek().canvasGroup.interactable = false;
                    activeMenus.Peek().canvasGroup.blocksRaycasts = false;
                }
                else
                {
                    if (blockParent)
                    {
                        activeMenus.Peek().canvasGroup.interactable = false;
                        activeMenus.Peek().canvasGroup.blocksRaycasts = false;
                    }
                }


            }

            menu.Open();
            activeMenus.Push(menu);
        }
    }
}