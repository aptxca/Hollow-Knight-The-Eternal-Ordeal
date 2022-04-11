using System;
using System.Collections.Generic;
using UnityEngine;


namespace Demo2D
{
    public enum SpawnType
    {
        Top,
        Mid,
        Bottom,
        Random
    }

    [Serializable]
    public class EnemySpawnItem
    {
        public string path;
        public int startSpawnKillCount;
        public int maxOnStageCount;
        public float spawnInterval;
        public SpawnType type;

    }

    [CreateAssetMenu(fileName = "EnemyCountSetting", menuName = "Enemy Count Setting")]
    public class EnemySpawnSetting : ScriptableObject
    {
        public List<EnemySpawnItem> settings;
    }
}