using System.Collections.Generic;
using Forgefall.Config;
using UnityEngine;

namespace Forgefall.Waves
{
    // Procedurally extends a difficulty profile's authored waves (DifficultyProfile.IsValid
    // requires exactly 6, frozen per Stage 4 turret balance) indefinitely for endless play.
    // Waves 1-6 are never touched: WaveDirector defers to DifficultyProfile.GetWave for any
    // waveNumber within the authored range and only calls Generate beyond it. Every 10th wave
    // (10, 20, 30, ...) replaces that wave's squad with a single oversized "boss" enemy instead.
    //
    // The per-wave growth rate (enemy health, prep time, enemy count) is derived directly from
    // each difficulty's own authored wave 5 -> wave 6 delta, per design decision (2026-09-17):
    // continue each difficulty's real curve rather than inventing a new one. concurrentLaneLimit
    // freezes at wave 6's value -- more lanes were never part of the turret-balance freeze's
    // premise, and the arena has exactly 4 lanes regardless. Boss stat multipliers (health x15,
    // contact damage x3, visual scale x2.5) are a concrete first pass, not a tuned/validated
    // balance number -- flagged as provisional pending an actual endless playtest.
    public static class EndlessWaveGenerator
    {
        public const int BossWaveInterval = 10;
        private const int BossHealthMultiplier = 15;
        private const int BossContactDamageMultiplier = 3;
        private const float BossVisualScale = 2.5f;

        // Base per-lane batch cap (see BuildGroups) and how fast it grows past the authored
        // range. 2026-09-18 live playtest: on Normal (concurrentLaneLimit frozen at 3) the fixed
        // prefix-iteration below always picked Lanes[0..2] = North/East/South, so West never
        // spawned a single enemy for the entire endless run -- worse on Easy (limit 2: only
        // North/East). Fixed by rotating which lanes a batch starts from instead of always
        // starting at index 0, so every lane cycles back in over a few waves regardless of how
        // small concurrentLaneLimit is. Separately, per-lane batch density (perLaneBatchCap) was
        // hardcoded at the authored max of 3 forever, so instead of a single batch ever getting
        // denser as an endless run progresses, BuildGroups just kept adding more, smaller-spaced
        // batches without bound. Growing perLaneBatchCap slowly (+1 every WavesPerCapIncrease
        // waves past wave 6, mirroring how enemyMaxHealth keeps extrapolating) keeps that in
        // check instead of leaving it flat forever.
        // Stage 7C.1 (2026-09-23 Hard endless playtest: "late waves too easy, want more enemies
        // arriving row after row"): the row width (per-lane batch cap) now grows every 10 waves
        // instead of 5, the enemy-count slope past the authored waves is 1.5x the authored
        // wave 5 -> 6 delta, and batches arrive as assaults of RowsPerAssault rows RowInterval
        // seconds apart (the previous batch spacing now separates assaults). Waves 1-6 are
        // untouched.
        private const int BaseLaneBatchCap = 3;
        private const int WavesPerCapIncrease = 10;
        public const float CountGrowthMultiplier = 1.5f;
        public const int RowsPerAssault = 3;
        public const float RowInterval = 1.5f;

        // 2026-09-28 endless playtest request: every boss killed from wave 20 on (20, 30, 40, ...)
        // doubles the enemy count of the following normal waves (compounding: x2, x4, x8, ...),
        // adds one row to every assault (3 -> 4 -> 5, row width unchanged) and makes the next boss
        // x3 tougher (compounding on top of BossHealthMultiplier; x5 until the 2026-09-30 playtest
        // found the wave-40 boss far too tanky). Counted from the wave number:
        // wave N > 20 only starts after the boss of every earlier boss wave died. Waves 1-20
        // and the wave-10/20 bosses are unchanged.
        public const int GrowthBossStartWave = 20;
        public const int BossCountMultiplier = 2;
        public const int BossRowIncrease = 1;
        public const int BossHealthGrowth = 3;
        // 2026-09-29 owner: from the first growth boss on the row width (enemies per lane in one row)
        // doubles per growth boss instead of the slow +1 every 10 waves: 3 -> 6 after the wave-20 boss,
        // 12 after wave 30. Capped at MaxRowWidth so a row (1.5 m spacing) stays inside the lane mouth.
        public const int RowWidthGrowth = 2;
        public const int MaxRowWidth = 12;
        // Overflow guard only (x1024 enemies / boss HP x3^10); no run gets near it.
        private const int MaximumGrowthSteps = 10;

        private static readonly AttackLane[] Lanes =
            { AttackLane.North, AttackLane.East, AttackLane.South, AttackLane.West };

        public readonly struct GeneratedWave
        {
            public GeneratedWave(WaveConfig.Wave wave, bool isBossWave)
            {
                Wave = wave;
                IsBossWave = isBossWave;
            }

            public WaveConfig.Wave Wave { get; }
            public bool IsBossWave { get; }
        }

        public static bool IsBossWaveNumber(int waveNumber) => waveNumber > 0 && waveNumber % BossWaveInterval == 0;

        // waveNumber is 1-based. Callers must resolve waves within the authored range
        // (1..profile.WaveCount) via DifficultyProfile.GetWave themselves -- this only
        // covers the procedural extension beyond it.
        public static GeneratedWave Generate(DifficultyProfile profile, int waveNumber)
        {
            int waveCount = profile.WaveCount;
            WaveConfig.Wave last = profile.GetWave(waveCount - 1);
            WaveConfig.Wave secondLast = profile.GetWave(waveCount - 2);

            int healthDelta = last.enemyMaxHealth - secondLast.enemyMaxHealth;
            float preparationDelta = last.preparation - secondLast.preparation;
            int countDelta = last.EnemyCount - secondLast.EnemyCount;
            int laneLimit = last.ConcurrentLaneLimit;
            int stepsPastAuthored = waveNumber - waveCount;

            int enemyMaxHealth = Mathf.Max(1, last.enemyMaxHealth + healthDelta * stepsPastAuthored);
            float preparation = Mathf.Max(1f, last.preparation + preparationDelta * stepsPastAuthored);

            int growthSteps = GrowthBossesDefeatedBefore(waveNumber);

            if (IsBossWaveNumber(waveNumber))
            {
                var bossGroup = new[]
                {
                    new WaveConfig.SpawnGroup { lane = BossLane(waveNumber), enemyCount = 1, spawnTime = 0f }
                };
                long bossHealth = (long)enemyMaxHealth * BossHealthMultiplier * Power(BossHealthGrowth, growthSteps);
                int clampedBossHealth = (int)System.Math.Clamp(bossHealth, 1L, int.MaxValue);
                return new GeneratedWave(WaveConfig.Wave.CreateProcedural(clampedBossHealth, preparation, 1, bossGroup), true);
            }

            int baseEnemyCount = Mathf.Max(1,
                last.EnemyCount + Mathf.RoundToInt(countDelta * CountGrowthMultiplier * stepsPastAuthored));
            int totalEnemyCount = (int)System.Math.Min(int.MaxValue / 2,
                (long)baseEnemyCount * Power(BossCountMultiplier, growthSteps));
            int perLaneBatchCap = RowWidthForWave(waveNumber, stepsPastAuthored);
            int rowsPerAssault = RowsPerAssault + BossRowIncrease * growthSteps;
            WaveConfig.SpawnGroup[] groups = BuildGroups(totalEnemyCount, laneLimit, preparation, perLaneBatchCap,
                rowsPerAssault, waveNumber);
            return new GeneratedWave(WaveConfig.Wave.CreateProcedural(enemyMaxHealth, preparation, laneLimit, groups), false);
        }

        // Bosses killed on growth boss waves (20, 30, ...) before this wave starts: 0 up to wave 20,
        // 1 for waves 21-30, 2 for 31-40, ... (clamped by MaximumGrowthSteps).
        public static int GrowthBossesDefeatedBefore(int waveNumber)
        {
            if (waveNumber <= GrowthBossStartWave) return 0;
            int steps = (waveNumber - 1) / BossWaveInterval - (GrowthBossStartWave / BossWaveInterval - 1);
            return Mathf.Clamp(steps, 0, MaximumGrowthSteps);
        }

        // Enemies per lane in one row: 3 (+1 every 10 waves past the authored ones) until the wave-20 boss,
        // then 3 x 2^growthSteps (6, 12), capped at MaxRowWidth.
        public static int RowWidthForWave(int waveNumber, int stepsPastAuthored)
        {
            int growthSteps = GrowthBossesDefeatedBefore(waveNumber);
            if (growthSteps == 0) return BaseLaneBatchCap + Mathf.Max(0, stepsPastAuthored) / WavesPerCapIncrease;
            return (int)System.Math.Min(MaxRowWidth, BaseLaneBatchCap * Power(RowWidthGrowth, growthSteps));
        }

        public static int RowsPerAssaultForWave(int waveNumber) =>
            RowsPerAssault + BossRowIncrease * GrowthBossesDefeatedBefore(waveNumber);

        private static long Power(int value, int exponent)
        {
            long result = 1;
            for (int i = 0; i < exponent; i++) result *= value;
            return result;
        }

        public static int ResolveContactDamage(int baseContactDamage, bool isBossWave) =>
            isBossWave ? Mathf.Max(1, baseContactDamage * BossContactDamageMultiplier) : baseContactDamage;

        public static float ResolveVisualScale(bool isBossWave) => isBossWave ? BossVisualScale : 1f;

        private static AttackLane BossLane(int waveNumber)
        {
            int index = waveNumber / BossWaveInterval - 1;
            return Lanes[index % Lanes.Length];
        }

        // Every authored wave (all three difficulties, waves 1-6) caps a single spawn batch at
        // 3 enemies per lane -- the heaviest any hand-authored wave ever asked one lane to
        // absorb at once (verified against every waves[] entry in Easy/Normal/Hard.asset).
        // The original two-batch split below ignored that cap and packed the whole extrapolated
        // total into just two batches, so as soon as an endless wave's count grew past
        // 2 x (laneLimit x cap) a single batch could dump way more enemies into one lane
        // simultaneously than any authored wave had ever produced. Turrets only ever engage one
        // target at a time (Turret.cs), so a denser-than-ever lane queue looks like turrets
        // "lagging" even though per-frame target acquisition itself has no delay -- see the
        // 2026-09-17 wave 7 playtest report. Fix: keep adding more, smaller batches (spaced like
        // the authored mid-wave reinforcement, ~preparation * 0.12s apart) instead of stuffing
        // more into each one, so no lane ever receives more than perLaneBatchCap in a single
        // batch (perLaneBatchCap itself now grows slowly past wave 6 -- see Generate above --
        // instead of staying frozen at the authored max forever).
        private static WaveConfig.SpawnGroup[] BuildGroups(int totalEnemyCount, int laneLimit, float preparation,
            int perLaneBatchCap, int rowsPerAssault, int waveNumber)
        {
            rowsPerAssault = Mathf.Max(1, rowsPerAssault);
            laneLimit = Mathf.Clamp(laneLimit, 1, Lanes.Length);
            int batchCap = laneLimit * perLaneBatchCap;
            int batchCount = Mathf.Max(1, Mathf.CeilToInt(totalEnemyCount / (float)batchCap));
            float assaultSpacing = Mathf.Max(4f, preparation * 0.12f,
                (rowsPerAssault - 1) * RowInterval + 1f);

            var groups = new List<WaveConfig.SpawnGroup>();
            int remaining = totalEnemyCount;
            for (int batch = 0; batch < batchCount; batch++)
            {
                int batchesLeft = batchCount - batch;
                int thisBatch = Mathf.Min(batchCap, Mathf.CeilToInt(remaining / (float)batchesLeft));
                // Rotate which lanes each assault starts from instead of always starting at
                // Lanes[0], so a laneLimit smaller than Lanes.Length still cycles through every
                // lane over a few waves/assaults rather than permanently dropping whichever ones
                // sit past index (laneLimit - 1). The rows of one assault share their lanes, so
                // each lane sees row 1, row 2, row 3 in succession.
                int assault = batch / rowsPerAssault;
                int row = batch % rowsPerAssault;
                int laneOffset = (waveNumber + assault) % Lanes.Length;
                AddBatch(groups, thisBatch, laneLimit, assault * assaultSpacing + row * RowInterval, laneOffset);
                remaining -= thisBatch;
            }
            return groups.ToArray();
        }

        private static void AddBatch(List<WaveConfig.SpawnGroup> groups, int count, int laneLimit, float spawnTime,
            int laneOffset)
        {
            int perLane = count / laneLimit;
            int remainder = count % laneLimit;
            for (int slot = 0; slot < laneLimit; slot++)
            {
                int laneCount = perLane + (slot < remainder ? 1 : 0);
                if (laneCount <= 0) continue;
                int laneIndex = (laneOffset + slot) % Lanes.Length;
                groups.Add(new WaveConfig.SpawnGroup { lane = Lanes[laneIndex], enemyCount = laneCount, spawnTime = spawnTime });
            }
        }
    }
}