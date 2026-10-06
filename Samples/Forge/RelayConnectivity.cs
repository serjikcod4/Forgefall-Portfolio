using System;
using System.Collections.Generic;
using UnityEngine;

namespace Forgefall.Forge
{
    public sealed class RelayConnectivityResult
    {
        internal RelayConnectivityResult(bool[] connectedSources, HashSet<HexCoordinate> poweredCells,
            Dictionary<HexCoordinate, int> coverageCounts)
        {
            ConnectedSources = connectedSources;
            PoweredCells = poweredCells;
            CoverageCounts = coverageCounts;
        }

        public IReadOnlyList<bool> ConnectedSources { get; }
        public HashSet<HexCoordinate> PoweredCells { get; }

        // Number of distinct Forge-connected sources whose coverage includes each
        // cell. Used by ResonantGrid (a cell is RESONANT at count >= 2); computed
        // unconditionally since it is a pure spatial fact, independent of research.
        public IReadOnlyDictionary<HexCoordinate, int> CoverageCounts { get; }
    }

    public static class RelayConnectivity
    {
        static readonly HexCoordinate[] Neighbours =
        {
            new HexCoordinate(0, 1),
            new HexCoordinate(-1, 1),
            new HexCoordinate(-1, 0),
            new HexCoordinate(0, -1),
            new HexCoordinate(1, -1),
            new HexCoordinate(1, 0)
        };

        public static RelayConnectivityResult Resolve(
            IReadOnlyList<HexCoordinate> origins,
            IReadOnlyList<int> radii,
            int forgeSourceIndex,
            Predicate<HexCoordinate> isInArena,
            bool forgeAlive)
        {
            int count = origins != null ? origins.Count : 0;
            if (radii == null || radii.Count != count || isInArena == null)
                throw new ArgumentException("Relay connectivity inputs must have matching source lists and an arena predicate.");

            var coverage = new List<HashSet<HexCoordinate>>(count);
            for (int i = 0; i < count; i++)
            {
                var cells = new HashSet<HexCoordinate>();
                AddCoverage(cells, origins[i], Mathf.Max(0, radii[i]), isInArena);
                coverage.Add(cells);
            }

            var connected = new bool[count];
            var powered = new HashSet<HexCoordinate>();
            var coverageCounts = new Dictionary<HexCoordinate, int>();
            if (!forgeAlive || forgeSourceIndex < 0 || forgeSourceIndex >= count)
                return new RelayConnectivityResult(connected, powered, coverageCounts);

            var queue = new Queue<int>(count);
            connected[forgeSourceIndex] = true;
            queue.Enqueue(forgeSourceIndex);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int candidate = 0; candidate < count; candidate++)
                {
                    if (connected[candidate] || !RegionsConnect(coverage[current], coverage[candidate])) continue;
                    connected[candidate] = true;
                    queue.Enqueue(candidate);
                }
            }

            for (int i = 0; i < count; i++)
            {
                if (!connected[i]) continue;
                powered.UnionWith(coverage[i]);
                foreach (HexCoordinate cell in coverage[i])
                    coverageCounts[cell] = coverageCounts.TryGetValue(cell, out int existing) ? existing + 1 : 1;
            }
            return new RelayConnectivityResult(connected, powered, coverageCounts);
        }

        public static bool RegionsConnect(
            IReadOnlyCollection<HexCoordinate> first,
            IReadOnlyCollection<HexCoordinate> second)
        {
            if (first == null || second == null || first.Count == 0 || second.Count == 0) return false;
            var lookup = second as HashSet<HexCoordinate> ?? new HashSet<HexCoordinate>(second);
            foreach (HexCoordinate cell in first)
            {
                if (lookup.Contains(cell)) return true;
                for (int i = 0; i < Neighbours.Length; i++)
                    if (lookup.Contains(new HexCoordinate(
                        cell.Q + Neighbours[i].Q,
                        cell.R + Neighbours[i].R))) return true;
            }
            return false;
        }

        public static void AddCoverage(
            HashSet<HexCoordinate> destination,
            HexCoordinate origin,
            int radius,
            Predicate<HexCoordinate> isInArena)
        {
            if (destination == null || isInArena == null) return;
            for (int q = -radius; q <= radius; q++)
            {
                int minimumR = Mathf.Max(-radius, -q - radius);
                int maximumR = Mathf.Min(radius, -q + radius);
                for (int r = minimumR; r <= maximumR; r++)
                {
                    var coordinate = new HexCoordinate(origin.Q + q, origin.R + r);
                    if (isInArena(coordinate)) destination.Add(coordinate);
                }
            }
        }
    }
}
