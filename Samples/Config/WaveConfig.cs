using System;
using UnityEngine;

namespace Forgefall.Config
{
    public enum AttackLane { North, East, South, West }

    [CreateAssetMenu(menuName = "Forgefall/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [Serializable]
        public struct SpawnGroup
        {
            public AttackLane lane;
            [Min(1)] public int enemyCount;
            [Min(0f)] public float spawnTime;
        }

        [Serializable]
        public struct Wave
        {
            [Min(1)] public int enemyMaxHealth;
            [Min(0f)] public float preparation;
            [Min(1)] public int concurrentLaneLimit;
            [SerializeField] private SpawnGroup[] groups;

            public int GroupCount => groups == null ? 0 : groups.Length;
            public int ConcurrentLaneLimit => Mathf.Max(1, concurrentLaneLimit);
            public SpawnGroup GetGroup(int index) => groups[index];
            public int EnemyCount
            {
                get
                {
                    int total = 0;
                    if (groups == null) return total;
                    for (int i = 0; i < groups.Length; i++)
                        total += Mathf.Max(0, groups[i].enemyCount);
                    return total;
                }
            }

            public bool UsesLane(AttackLane lane)
            {
                if (groups == null) return false;
                for (int i = 0; i < groups.Length; i++)
                    if (groups[i].enemyCount > 0 && groups[i].lane == lane) return true;
                return false;
            }

            // Builds a Wave at runtime for the endless-mode extension beyond the authored
            // 6-wave scenario (see Waves/EndlessWaveGenerator.cs). Never used by authored
            // ScriptableObject data, which Unity deserializes directly.
            public static Wave CreateProcedural(int enemyMaxHealth, float preparation,
                int concurrentLaneLimit, SpawnGroup[] groups)
            {
                return new Wave
                {
                    enemyMaxHealth = enemyMaxHealth,
                    preparation = preparation,
                    concurrentLaneLimit = concurrentLaneLimit,
                    groups = groups ?? Array.Empty<SpawnGroup>()
                };
            }
        }

        [SerializeField] private Wave[] waves;
        public int Count => waves == null ? 0 : waves.Length;
        public Wave Get(int index) => waves[index];
    }
}