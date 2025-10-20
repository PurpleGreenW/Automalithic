using System;

// Ein einfacher 3D-Integer-Vektor (x,y,z) mit Gleichheits- und HashCode-Unterstützung
public struct Vector3Int : IEquatable<Vector3Int>
{
    public int x;
    public int y;
    public int z;

    public Vector3Int(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public bool Equals(Vector3Int other)
    {
        return x == other.x && y == other.y && z == other.z;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector3Int other && Equals(other);
    }

    public override int GetHashCode()
    {
        // einfache, aber recht gute Hash-Verteilung
        return (x * 73856093) ^ (y * 19349669) ^ (z * 83492791);
    }

    public static Vector3Int operator +(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3Int operator -(Vector3Int a, Vector3Int b)
    {
        return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public override string ToString() => $"({x},{y},{z})";
}