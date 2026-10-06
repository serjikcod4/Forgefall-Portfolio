using System;
using UnityEngine;

namespace Forgefall.Forge
{
    [Serializable]
    public struct HexCoordinate : IEquatable<HexCoordinate>
    {
        [SerializeField] private int q;
        [SerializeField] private int r;

        public HexCoordinate(int q, int r) { this.q = q; this.r = r; }
        public int Q => q;
        public int R => r;
        public int S => -q - r;
        public static HexCoordinate Zero => new HexCoordinate(0, 0);

        public int DistanceTo(HexCoordinate other)
        {
            int dq = Mathf.Abs(q - other.q);
            int dr = Mathf.Abs(r - other.r);
            int ds = Mathf.Abs(S - other.S);
            return (dq + dr + ds) / 2;
        }

        public bool Equals(HexCoordinate other) => q == other.q && r == other.r;
        public override bool Equals(object obj) => obj is HexCoordinate other && Equals(other);
        public override int GetHashCode() { unchecked { return (q * 397) ^ r; } }
        public override string ToString() => $"({q},{r})";
        public static bool operator ==(HexCoordinate left, HexCoordinate right) => left.Equals(right);
        public static bool operator !=(HexCoordinate left, HexCoordinate right) => !left.Equals(right);
    }
}
