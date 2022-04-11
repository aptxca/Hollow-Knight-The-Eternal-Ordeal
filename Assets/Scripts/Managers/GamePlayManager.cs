using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Demo2D;
using Demo2D.Frame;
using UnityEngine;

namespace Demo2D
{

    public class GamePlayManager : MonoBehaviour
    {
        public bool autoStart;
        public Interactable interactable;

        [Header("怪物相关")] public string enemySpawnSettingPath;
        public EnemySpawnSetting spawnSetting;
        public Transform enemyTrans;
        public Transform topSpawnPoint;
        public Transform midSpawnPoint;
        public Transform bottomSpawnPoint;
        public float randomX;
        public float randomY;



        public ReactiveProperty<int> killCount = new ReactiveProperty<int>(0);
        private IEnumerator _spawnCoroutine;

        private Dictionary<EnemySpawnItem, List<GameObjectResource>> _enemyDic =
            new Dictionary<EnemySpawnItem, List<GameObjectResource>>();


        public void Init()
        {
            spawnSetting = ResourceManager.Instance.LoadResource<EnemySpawnSetting>(enemySpawnSettingPath);
            if (enemyTrans == null)
            {
                GameObject enemies = new GameObject("_Enemies");
                enemyTrans = enemies.transform;
            }
            PreloadEnemies(spawnSetting);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterScore(killCount);
            }
            killCount.Data = 0;

            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.RegisterDeathEvent(OnHeroKilled);
            }
            else
            {
                Debug.Log("找不到player");
            }
        }

        public void StartGamePlay()
        {
            Init();
            _spawnCoroutine = EnemySpawnCoroutine();
            StartCoroutine(_spawnCoroutine);
        }

        public void StopGamePlay()
        {
            StopCoroutine(_spawnCoroutine);
        }

        private IEnumerator EnemySpawnCoroutine()
        {
            List<float> times = new List<float>();

            for (int i = 0; i < spawnSetting.settings.Count; i++)
            {
                times.Add(0f);
            }

            float deltaTime;
            while (true)
            {
                deltaTime = Time.deltaTime;
                for (int i = 0; i < _enemyDic.Keys.Count; i++)
                {
                    times[i] += deltaTime;
                    var spawnItem = _enemyDic.Keys.ElementAt(i);
                    if (times[i] > spawnItem.spawnInterval && killCount.Data >= spawnItem.startSpawnKillCount)
                    {
                        SpawnEnemy(spawnItem);
                        times[i] = 0f;
                    }
                }

                yield return null;
            }
        }

        public void SpawnEnemy(EnemySpawnItem item)
        {
            if (!_enemyDic.TryGetValue(item, out var objResList) || objResList == null ||
                objResList.Count(objRes => !objRes.inUse) == 0)
            {
                return;
            }

            GameObject obj = GameObjectManager.Instance.InstantiateGameObject(item.path);
            switch (item.type)
            {
                case SpawnType.Top:
                    obj.transform.position = topSpawnPoint.position + Vector3.right * Random.Range(-randomX, randomX);
                    break;
                case SpawnType.Mid:
                    obj.transform.position = midSpawnPoint.position + Vector3.right * Random.Range(-randomX, randomX) +
                                             Vector3.up * Random.Range(-randomY, randomY);
                    break;
                case SpawnType.Bottom:
                    obj.transform.position = bottomSpawnPoint.position + Vector3.right * Random.Range(-randomX, randomX);
                    break;
            }
        }

        public void PreloadEnemies(EnemySpawnSetting enemySpawnSetting)
        {
            foreach (var spawnItem in enemySpawnSetting.settings)
            {
                if (!_enemyDic.ContainsKey(spawnItem))
                {
                    _enemyDic.Add(spawnItem, new List<GameObjectResource>(spawnItem.maxOnStageCount));
                }

                for (int i = 0; i < spawnItem.maxOnStageCount; i++)
                {
                    GameObjectResource objRes = GameObjectManager.Instance.InstantiateGameObjectRes(spawnItem.path);
                    if (objRes != null && objRes.objClone != null)
                    {
                        _enemyDic[spawnItem].Add(objRes);
                    }
                }
            }

            foreach (var objResList in _enemyDic.Values)
            {
                if (objResList == null)
                {
                    continue;
                }

                foreach (var objRes in objResList)
                {
                    if (objRes.objClone == null)
                    {
                        continue;
                    }

                    objRes.objClone.transform.SetParent(enemyTrans, false);
                    EnemyController controller = objRes.objClone.GetComponent<EnemyController>();
                    if (controller == null)
                    {
                        Debug.Log(objRes.objClone.name + "没有角色控制器");
                        continue;
                    }
                    controller.RegisterDeathEvent(OnEnemyKilled);
                    GameObjectManager.Instance.Recycle(objRes.objClone);
                }
            }

        }

        public void OnEnemyKilled()
        {
            killCount.Data++;
        }
        public void OnHeroKilled()
        {
            GameManager.Instance.GameOver(killCount.Data);
            killCount.Data = 0;
            killCount.onValueChanged = null;
            PlayerController.Instance.UnRegisterDeathEvent(OnHeroKilled);
        }

    }
}