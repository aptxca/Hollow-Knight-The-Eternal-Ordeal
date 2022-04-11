using Demo2D;
using UnityEngine;

namespace Demo2D
{
    public class TargetScanner : MonoBehaviour
    {
        public float findTargetDistance;
        public float lostTargetDistance;
        public bool FindTarget { get; protected set; }
        public Transform target;
        public float TargetDistance { get; protected set; }

        private float _lostSqr;
        private float _findSqr;
        void Update()
        {
            if (target == null)
            {
                target = PlayerController.Instance.transform;
                FindTarget = false;
                return;
            }

            TargetDistance = (target.position - transform.position).magnitude;
            if (FindTarget)
            {
                if (TargetDistance > lostTargetDistance)
                {
                    FindTarget = false;
                }
            }
            else
            {
                if (TargetDistance < findTargetDistance)
                {
                    FindTarget = true;
                }
            }
        }
    }
}