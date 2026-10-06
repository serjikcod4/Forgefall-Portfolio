using System.Collections.Generic;
using System.Linq;
using Forgefall.Forge;
using NUnit.Framework;

namespace Forgefall.Portfolio.Tests
{
    // Adapted from the private project's StaticAndPureGraph checks.
    // Scene assertions are excluded so these cases can run in a fresh project.
    public sealed class RelayConnectivityTests
    {
        [Test]
        public void RegionsConnectThroughOverlapOrSharedEdgeButNotAGap()
        {
            HashSet<HexCoordinate> forge = Disc(HexCoordinate.Zero);
            Assert.That(RelayConnectivity.RegionsConnect(forge, Disc(new HexCoordinate(2, 0))), Is.True);
            Assert.That(RelayConnectivity.RegionsConnect(forge, Disc(new HexCoordinate(3, 0))), Is.True);
            Assert.That(RelayConnectivity.RegionsConnect(forge, Disc(new HexCoordinate(4, 0))), Is.False);
        }

        [Test]
        public void IsolatedCycleDoesNotSupplyPower()
        {
            var origins = new[]
            {
                HexCoordinate.Zero, new HexCoordinate(8, 0),
                new HexCoordinate(9, 0), new HexCoordinate(8, 1)
            };
            RelayConnectivityResult result = RelayConnectivity.Resolve(origins, new[] { 1, 1, 1, 1 }, 0, _ => true, true);
            CollectionAssert.AreEqual(new[] { true, false, false, false }, result.ConnectedSources);
            CollectionAssert.AreEquivalent(Disc(HexCoordinate.Zero), result.PoweredCells);
        }

        [Test]
        public void EitherAlternativeBridgeCanBeRemovedWithoutLosingPower()
        {
            var origins = new[]
            {
                HexCoordinate.Zero, new HexCoordinate(2, 0),
                new HexCoordinate(3, 0), new HexCoordinate(5, 0)
            };
            RelayConnectivityResult initial = RelayConnectivity.Resolve(origins, new[] { 1, 1, 1, 1 }, 0, _ => true, true);
            Assert.That(initial.ConnectedSources.All(value => value), Is.True);
            for (int bridge = 1; bridge <= 2; bridge++)
            {
                HexCoordinate[] remaining = origins.Where((_, index) => index != bridge).ToArray();
                RelayConnectivityResult result = RelayConnectivity.Resolve(remaining, new[] { 1, 1, 1 }, 0, _ => true, true);
                Assert.That(result.ConnectedSources.All(value => value), Is.True);
            }
        }

        [Test]
        public void OverlappingConnectedSourcesCountOnceEach()
        {
            var origins = new[] { HexCoordinate.Zero, new HexCoordinate(1, 0), new HexCoordinate(8, 0) };
            RelayConnectivityResult result = RelayConnectivity.Resolve(origins, new[] { 1, 1, 1 }, 0, _ => true, true);
            Assert.That(result.CoverageCounts[HexCoordinate.Zero], Is.EqualTo(2));
            Assert.That(result.CoverageCounts.ContainsKey(new HexCoordinate(8, 0)), Is.False);
        }

        [Test]
        public void DeadForgeLeavesAllSourcesDisconnected()
        {
            RelayConnectivityResult result = RelayConnectivity.Resolve(
                new[] { HexCoordinate.Zero, new HexCoordinate(1, 0) }, new[] { 1, 1 }, 0, _ => true, false);
            Assert.That(result.ConnectedSources.All(value => !value), Is.True);
            Assert.That(result.PoweredCells, Is.Empty);
            Assert.That(result.CoverageCounts, Is.Empty);
        }

        private static HashSet<HexCoordinate> Disc(HexCoordinate origin)
        {
            var cells = new HashSet<HexCoordinate>();
            RelayConnectivity.AddCoverage(cells, origin, 1, _ => true);
            return cells;
        }
    }
}
