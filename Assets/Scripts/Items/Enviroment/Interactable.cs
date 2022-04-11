using System;
using System.Collections;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    public abstract class Interactable : MonoBehaviour
    {
        public string iconPath;
        public Vector3 offset;

        private GameObject icon;
        private Animator _animator;
        private Collider2D _collider;

        public abstract bool CheckCondition();

        public void Interact()
        {
            GameManager.Instance.LoadScene(3,true);
            PlayerController.Instance.LoseInteractable(this);
            GameObject.Destroy(this);
        }

        public void Start()
        {
            if (String.IsNullOrEmpty(iconPath))
            {
                return;
            }

            icon = UIManager.Instance.CreateUIObject(iconPath,
                Camera.main.WorldToScreenPoint(transform.position + offset));
            if (icon != null)
            {
                icon.SetActive(false);
                _animator = icon.GetComponent<Animator>();
            }

        }

        public virtual void OnTriggerEnter2D(Collider2D col)
        {
            if (icon != null)
            {
                UIManager.Instance.SetUIObjectPosition(icon.transform as RectTransform,
                    Camera.main.WorldToScreenPoint(transform.position + offset));
                icon.SetActive(true);
                PlayerController.Instance.FindInteractable(this);
            }

        }

        public virtual void OnTriggerStay2D(Collider2D col)
        {
            if (icon != null)
            {
                UIManager.Instance.SetUIObjectPosition(icon.transform as RectTransform,
                    Camera.main.WorldToScreenPoint(transform.position + offset));
            }
        }

        public virtual void OnTriggerExit2D(Collider2D col)
        {
            StartCoroutine(CloseCoroutine());
            PlayerController.Instance.LoseInteractable(this);
        }

        private IEnumerator CloseCoroutine()
        {
            if (_animator!=null)
            {
                _animator.Play("Close",-1,0);
                yield return null;

                while (!_animator.IsCurrentStateEnd(0))
                {
                    yield return null;
                }
            }

            icon.SetActive(false);
        }

        public void OnDestroy()
        {
            if (icon!=null)
            {
                UIManager.Instance.RemoveUIObject(icon,true);
            }
        }

    }
}