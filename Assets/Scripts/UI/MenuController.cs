using System.Collections;
using System.Collections.Generic;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    public enum MenuType
    {
        MainMenu,
        Hud,
        PauseMenu,
        OptionMenu,
        AchievementMenu,
        AddonMenu,
    }


    public class MenuController : MonoBehaviour
    {
        public MenuType type;
        public List<SelectButton> buttons;
        public List<Animator> animators;
        public CanvasGroup canvasGroup;

        public void Start()
        {
           init();
        }

        protected virtual void init()
        {
            foreach (var button in buttons)
            {
                button.AddListener(OnButtonSendCmd);
            }
        }


        public void OnButtonSendCmd(SelectButton button)
        {

            switch (button.cmd)
            {
                case UiButtonCmd.Start:
                case UiButtonCmd.Resume:
                case UiButtonCmd.BackToMainMenu:
                case UiButtonCmd.ExitGame:
                    UIManager.Instance.ApplyCmd(button.cmd);
                    break;
                case UiButtonCmd.GoToChildMenu:
                    UIManager.Instance.OpenMenu(button.nextMenuType, true, false, true);
                    break;
                case UiButtonCmd.BackToParentMenu:
                    UIManager.Instance.BackToParentMenu(this);
                    break;
                case UiButtonCmd.ClossMenu:
                    break;
                case UiButtonCmd.NextScene:
                    break;
                case UiButtonCmd.Confirm:
                    break;
                case UiButtonCmd.Revert:
                    break;
            }
        }

        public virtual IEnumerator Close()
        {
              yield return StartCoroutine(CloseCoroutine());
        }

        public virtual void CloseImmediately()
        {
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        public void Open()
        {
            canvasGroup.alpha = 1;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            foreach (var animator in animators)
            {
                animator.Play("Open",0,0);
            }
        }


        protected IEnumerator CloseCoroutine()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            foreach (var animator in animators)
            {
                animator.Play("Close",0,0);
            }

            //下一帧才开始切换动画，需要等一帧再做判断
            yield return null;
            while (!IsAllAnimatorStop(animators))
            {
                yield return null;
            }
            canvasGroup.alpha = 0;
        }

        public static bool IsAllAnimatorStop(List<Animator> animators)
        {
            foreach (var animator in animators)
            {
                if (!animator.IsCurrentStateEnd(0))
                {
                    return false;
                }
            }

            return true;
        }
    }
}