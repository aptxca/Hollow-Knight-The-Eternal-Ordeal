using System;
using Demo2D.Frame;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class GroundDetector : MonoBehaviour, ITrigger
    {
        private bool _isGrounded = false;

        public bool IsGrounded
        {
            get => _isGrounded;
            protected set
            {
                if (_isGrounded != value)
                {
                    _isGrounded = value;
                    Trigger();
                }
            }
        }

        public Platform Plat { get; set; }
        public LayerMask groundMask;
        private Action<ITrigger> _onTriggered;

        public void OnEnable()
        {
            ResetStatus();
        }

        public void ResetStatus()
        {
            Plat = null;
        }

        public void OnTriggerEnter2D(Collider2D collider)
        {
            if (groundMask.ContainLayer(collider.gameObject.layer))
            {
                IsGrounded = true;
            }

            Platform temp = collider.gameObject.GetComponent<Platform>();
            if (temp != null)
            {
                Plat = temp;
            }
        }

        public void OnTriggerExit2D(Collider2D collider)
        {
            if (groundMask.ContainLayer(collider.gameObject.layer))
            {
                IsGrounded = false;
            }

            Platform temp = collider.gameObject.GetComponent<Platform>();
            if (temp != null && Plat == temp)
            {
                Plat = null;
            }

        }

        public void AddListener(Action<ITrigger> listener)
        {
            _onTriggered += listener;
        }

        public void RemoveListener(Action<ITrigger> listener)
        {
            _onTriggered -= listener;
        }

        public void Trigger()
        {
            _onTriggered?.Invoke(this);
        }

    }
}