// File: D:/Godot Projects/automalithic/src/Utils/NoiseGen.cs
using System;

namespace Automalithic.Utils
{
    /// <summary>
    /// Lightweight deterministic "noise" used for PoC visual testing.
    /// Replace with OpenSimplex / FastNoise later.
    /// Returns value in [0,1].
    /// </summary>
    public static class NoiseGen
    {
        public static float MixedNoise3D(float x, float y, float z, int seed = 1337)
        {
            // simple combination of trigonometric + hashed pseudo-random components for plausible variation
            double v1 = 0.5 + 0.5 * Math.Sin(0.1 * x + 0.15 * y + 0.07 * z + seed * 0.01);
            double v2 = HashNoise(x * 0.01, y * 0.01, z * 0.01, seed);
            double v3 = HashNoise(x * 0.005, y * 0.005, z * 0.005, seed + 31) * 0.6;
            double outv = v1 * 0.6 + v2 * 0.3 + v3 * 0.1;
            return (float)Math.Clamp(outv, 0.0, 1.0);
        }

        private static double HashNoise(double x, double y, double z, int seed)
        {
            // small deterministic hash based noise
            long xi = (long)Math.Floor(x * 1619);
            long yi = (long)Math.Floor(y * 31337);
            long zi = (long)Math.Floor(z * 6971);
            long n = (xi * 73856093) ^ (yi * 19349663) ^ (zi * 83492791) ^ (seed * 2654435761);
            n = (n << 13) ^ n;
            long nn = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return (double)nn / 2147483647.0;
        }
    }
}