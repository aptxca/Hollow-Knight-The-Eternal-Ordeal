using Cinemachine;
using UnityEngine;

namespace Demo2D
{
    public class CameraSetting : MonoBehaviour
    {
        public CinemachineVirtualCamera vmCamera;

        public void SetFollowTarget(Transform transform)
        {
            vmCamera.Follow = transform;
        }
    }
}