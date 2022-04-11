using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Demo2D
{
    [Serializable]
    public class WayPoint
    {
        public Vector3 position;
        public float waitTime;
    }

    public class Platform : MonoBehaviour
    {
        //平台表面速度，向右或向上为正
        public float surfaceSpeed;
        //平台自身移速，向右或向上为正
        public float  platSpeed;
        //平台移动路径点
        public List<WayPoint> wayPoints = new List<WayPoint>();

        public Vector3 CurrentPlatSpeed { get; set; }

        private int _targetIndex;

        void Start()
        {
            if (wayPoints.Count>1)
            {
                StartCoroutine(MoveCoroutine());
            }
        }

        private IEnumerator MoveCoroutine()
        {
            int startIndex = GetNearestWayPointIndex();
            Vector3 delta;
            Vector3 direction;
            for (int i = startIndex;; i=(i+1)%wayPoints.Count)
            {
                delta = wayPoints[i].position - transform.position;
                direction = delta.normalized;
                CurrentPlatSpeed = direction * platSpeed;
                while (!Arrived(wayPoints[i]))
                {
                    transform.Translate(CurrentPlatSpeed * Time.deltaTime);
                    yield return null;
                }
                CurrentPlatSpeed = Vector3.zero;
                yield return new WaitForSeconds(wayPoints[i].waitTime);
            }
        }

        private bool Arrived(WayPoint wayPoint)
        {
            return (wayPoint.position - transform.position).sqrMagnitude < 0.0001f;
        }

        private int GetNearestWayPointIndex()
        {
            int index = -1;
            float dis = Mathf.Infinity;
            float temp = 0f;
            Vector3 delta;
            for (int i = 0; i <wayPoints.Count ; i++)
            {
                delta = wayPoints[i].position - transform.position;
                temp = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
                if (dis>=temp)
                {
                    dis = temp;
                    index = i;
                }
            }

            return index;
        }
    }
}