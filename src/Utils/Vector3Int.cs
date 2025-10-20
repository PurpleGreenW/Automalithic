// File: D:/Godot Projects/automalithic/src/Utils/Vector3Int.cs
using System;

namespace Automalithic.Utils
{
    /// <summary>
    /// Small integer 3D vector used as dictionary key for chunks / sample grid.
    /// </summary>
    public partial struct Vector3Int
    {
        public int X;
        public int Y;
        public int Z;

        public Vector3Int(int x, int y, int z)
        {
            X = x; Y = y; Z = z;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3Int other) return other.X == X && other.Y == Y && other.Z == Z;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X;
                hash = hash * 31 + Y;
                hash = hash * 31 + Z;
                return hash;
            }
        }

        public override string ToString() => $"({X},{Y},{Z})";
    }
}