using UnityEngine;

namespace Demo2D
{
    public class HealthOrb : MonoBehaviour
    {
        public float shiningInterval;
        public bool IsBroken { get; protected set; }

        private Animator _animator;
        private float _shinTime = 0f;

        public void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void Update()
        {
            if (_shinTime>shiningInterval&&!IsBroken)
            {
                _animator.Play("Shining",0,0);
                _shinTime = 0f;
            }

            _shinTime += Time.deltaTime;
        }

        public void SetState(bool unBroken)
        {
            if (unBroken&&IsBroken)
            {
                _animator.Play("Repair",0,0);
                IsBroken = false;
                return;
            }

            if (!unBroken&&!IsBroken)
            {
                _animator.Play("Break",0,0);
                IsBroken = true;
            }
        }
    }
}