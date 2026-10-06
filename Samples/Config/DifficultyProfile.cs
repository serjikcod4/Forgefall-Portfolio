using UnityEngine;

namespace Forgefall.Config
{
    [CreateAssetMenu(menuName = "Forgefall/Difficulty Profile")]
    public sealed class DifficultyProfile : ScriptableObject
    {
        [SerializeField] private string stableId = "normal";
        [SerializeField] private string displayName = "Normal";
        [TextArea(2, 3)]
        [SerializeField] private string description = "Balance gathering, building and defending.";
        [SerializeField, Min(1)] private int forgeMaximumHealth = 100;
        [SerializeField, Range(0.01f, 4f)] private float enemyContactDamageScale = 1f;
        [SerializeField, Min(0)] private int startingIron = 12;
        [SerializeField] private int[] postWaveIron;
        [SerializeField] private WaveConfig.Wave[] waves;

        // Economy v0.3 §5 elites (Stage 7A): from eliteFirstWave, eliteCountEarly elites per
        // non-boss wave; from eliteLateFromWave (0 = never) eliteCountLate instead. Elites
        // replace normal spawns, so a wave's enemy count is unchanged.
        [Header("Elites (Economy v0.3)")]
        [SerializeField, Min(1)] private int eliteFirstWave = 4;
        [SerializeField, Min(0)] private int eliteCountEarly = 1;
        [SerializeField, Min(0)] private int eliteLateFromWave;
        [SerializeField, Min(0)] private int eliteCountLate = 2;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public string Description => description;
        public int ForgeMaximumHealth => Mathf.Max(1, forgeMaximumHealth);
        public float EnemyContactDamageScale => Mathf.Max(0.01f, enemyContactDamageScale);
        public int StartingIron => Mathf.Max(0, startingIron);
        public int WaveCount => waves == null ? 0 : waves.Length;
        public int TotalPotentialIron
        {
            get
            {
                int total = StartingIron;
                if (postWaveIron == null) return total;
                for (int i = 0; i < postWaveIron.Length; i++) total += Mathf.Max(0, postWaveIron[i]);
                return total;
            }
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(stableId) && !string.IsNullOrWhiteSpace(displayName) &&
            WaveCount == 6 && postWaveIron != null && postWaveIron.Length == WaveCount;

        public WaveConfig.Wave GetWave(int index) => waves[index];

        public int GetPostWaveIron(int index)
        {
            return postWaveIron == null || index < 0 || index >= postWaveIron.Length
                ? 0
                : Mathf.Max(0, postWaveIron[index]);
        }

        public int EliteFirstWave => Mathf.Max(1, eliteFirstWave);
        public int EliteLateFromWave => Mathf.Max(0, eliteLateFromWave);

        public int ResolveEliteCount(int waveNumber, bool isBossWave)
        {
            if (isBossWave || waveNumber < EliteFirstWave) return 0;
            if (EliteLateFromWave > 0 && waveNumber >= EliteLateFromWave) return Mathf.Max(0, eliteCountLate);
            return Mathf.Max(0, eliteCountEarly);
        }

        public int ResolveEnemyContactDamage(int baselineDamage)
        {
            if (baselineDamage <= 0) return 0;
            return Mathf.Max(1, Mathf.FloorToInt(baselineDamage * EnemyContactDamageScale + 0.5f));
        }
    }
}
