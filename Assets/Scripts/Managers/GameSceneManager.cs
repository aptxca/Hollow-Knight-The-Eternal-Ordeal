using System.Collections.Generic;
using Demo2D.Utility;
using UnityEngine;

namespace Demo2D
{
    public class GameSceneManager : MonoSingleton<GameSceneManager>
    {
        public string heroResPath;
        public Transform heroRespawnTrans;

        public List<GamePlayManager> miniGameManagers;

        public override void Awake()
        {
            //GameSceneManager在当前场景中作为单例使用，切换场景后销毁
            CheckSingleton(false);
        }

        public void StartGameScene(bool resetDamage)
        {
            RespawnHero(resetDamage);
            foreach (var miniGame in miniGameManagers)
            {
                if (miniGame.autoStart)
                {
                    miniGame.StartGamePlay();
                }
            }
        }

        private void RespawnHero(bool resetDamage)
        {
            if (PlayerController.Instance==null)
            {
                PlayerController heroController = FindObjectOfType<PlayerController>();
                if (heroController ==null)
                {
                    GameObject heroObj = GameObjectManager.Instance.InstantiateGameObject(heroResPath,false);
                }
            }

            PlayerController.Instance.Respawn(heroRespawnTrans.position, resetDamage);
            if (PlayerInput.Instance != null)
            {
                PlayerInput.Instance.BlockInput = false;
            }
            CameraSetting cameraSetting = FindObjectOfType<CameraSetting>();
            if (cameraSetting!=null)
            {
                cameraSetting.SetFollowTarget(PlayerController.Instance.transform);
            }

        }

    }
}