using System;

// Modularer Noise-Provider (MVP)
public static class NoiseGen
{
    public static float ValueNoise3D(float x, float y, float z, int seed = 1337)
    {
        // Sehr einfache deterministische Pseudo-Random-Funktion als Platzhalter
        int n = (int)(x * 374761393 + y * 668265263 + z * 73856093) ^ seed;
        n = (n << 13) ^ n;
        float res = 1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
        return res;
    }

    // Einfacher Hilfs-Function, später erweiterbar
    public static float Mix(float a, float b, float t) => a * (1 - t) + b * t;
}