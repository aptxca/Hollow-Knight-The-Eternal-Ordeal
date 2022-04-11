using Demo2D;
using Demo2D.Utility;
using UnityEngine;


namespace Demo2D
{
    public class Explosion : MonoBehaviour
    {
        private ParticleSystem _particle;
        private AudioSource _audio;
        public LayerMask targetMask;
        public int damage;
        public int knokBack;

        public void Awake()
        {
            _particle = GetComponent<ParticleSystem>();
            _audio = GetComponent<AudioSource>();
        }

        public void OnEnable()
        {
            _particle.Play();
            _audio.Play();
        }

        public void Update()
        {
            if (!_particle.isPlaying)
            {
                GameObjectManager.Instance.Recycle(gameObject);
            }
        }

        public void OnTriggerEnter2D(Collider2D collider)
        {
            if (targetMask.ContainLayer(collider.gameObject.layer))
            {
                Damageable damageable = collider.gameObject.GetComponent<Damageable>();
                if (damageable!=null)
                {
                    damageable.ApplyDamage(new DamageData(this, gameObject,
                        collider.gameObject.transform.position - transform.position, damage, knokBack));
                }
            }
        }
    }
}