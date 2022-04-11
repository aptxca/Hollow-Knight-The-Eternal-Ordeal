using System;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Demo2D
{
    public enum UiButtonCmd
    {
        Resume,
        BackToMainMenu,
        ExitGame,
        NextScene,
        GoToChildMenu,
        BackToParentMenu,
        Confirm,
        Revert,
        ClossMenu,
        Start,
        None,
    }

    public class SelectButton : MonoBehaviour
    {
        public UiButtonCmd cmd;
        public MenuType nextMenuType;
        public GameObject leftIcon;
        public GameObject rightIcon;

        public Action<SelectButton> sendCommand;

        public void OnMousePointerEnter(BaseEventData data)
        {
            leftIcon.SetActive(true);
            rightIcon.SetActive(true);
        }

        public void OnMousePointerExit(BaseEventData data)
        {
            leftIcon.SetActive(false);
            rightIcon.SetActive(false);
        }

        public void OnMousePointerClick(BaseEventData data)
        {
            sendCommand?.Invoke(this);
        }

        public void AddListener(Action<SelectButton> listener)
        {
            sendCommand += listener;
        }

        public void RemoveListener(Action<SelectButton> listener)
        {
            sendCommand -= listener;
        }

    }
}