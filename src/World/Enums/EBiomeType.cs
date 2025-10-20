using Godot;

namespace Automalithic.World.Enums
{
    /// <summary>
    /// Definiert alle Biom-Typen in der Spielwelt.
    /// Biome beeinflussen die Verteilung von VoxelTypes und Umgebungsbedingungen.
    /// </summary>
    public enum EBiomeType : byte
    {
        // Basis-Biome
        Plains = 0,         // Ebenen mit Gras
        Forest = 1,         // Waldgebiet
        Desert = 2,         // Wüste
        Mountain = 3,       // Gebirge
        Ocean = 4,          // Ozean/große Wasserflächen
        
        // Spezial-Biome
        Jungle = 5,         // Dschungel (hohe Luftfeuchtigkeit)
        Tundra = 6,         // Tundra (kalt, wenig Vegetation)
        Swamp = 7,          // Sumpf (hohe Feuchtigkeit, Faulgase)
        Volcano = 8,        // Vulkanisch (Lava, heiße Gase)
        Cave = 9,           // Höhlensystem (unterirdisch)
        
        // Übergangs-Biome
        ForestEdge = 10,    // Waldrand
        BeachSand = 11,     // Strand
        RiverBank = 12,     // Flussufer
        
        // Ressourcen-reiche Biome
        IronRichMountain = 13,  // Eisenreiche Berge
        GoldRichDesert = 14,    // Goldreiche Wüste
        CrystalCave = 15,       // Kristallhöhle
    }

    /// <summary>
    /// Hilfsklasse für Biome-bezogene Operationen
    /// </summary>
    public static class BiomeTypeExtensions
    {
        /// <summary>
        /// Gibt die Basis-Luftfeuchtigkeit für dieses Biom zurück (0.0 - 1.0)
        /// </summary>
        public static float GetBaseHumidity(this EBiomeType biome)
        {
            return biome switch
            {
                EBiomeType.Desert => 0.1f,
                EBiomeType.Plains => 0.4f,
                EBiomeType.Forest => 0.6f,
                EBiomeType.Jungle => 0.9f,
                EBiomeType.Swamp => 0.95f,
                EBiomeType.Ocean => 0.8f,
                EBiomeType.Tundra => 0.2f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Gibt die Basis-Temperatur für dieses Biom zurück (0.0 - 1.0)
        /// </summary>
        public static float GetBaseTemperature(this EBiomeType biome)
        {
            return biome switch
            {
                EBiomeType.Desert => 0.9f,
                EBiomeType.Jungle => 0.8f,
                EBiomeType.Volcano => 1.0f,
                EBiomeType.Tundra => 0.1f,
                EBiomeType.Mountain => 0.3f,
                EBiomeType.Plains => 0.5f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Gibt die dominanten VoxelTypes für die Oberfläche dieses Bioms zurück
        /// </summary>
        public static EVoxelType[] GetSurfaceVoxelTypes(this EBiomeType biome)
        {
            return biome switch
            {
                EBiomeType.Desert => new[] { EVoxelType.Sand, EVoxelType.Sandstone },
                EBiomeType.Plains => new[] { EVoxelType.Grass, EVoxelType.Dirt },
                EBiomeType.Forest => new[] { EVoxelType.Grass, EVoxelType.Dirt, EVoxelType.Wood },
                EBiomeType.Mountain => new[] { EVoxelType.Stone, EVoxelType.Granite },
                EBiomeType.Ocean => new[] { EVoxelType.Sand, EVoxelType.Water },
                EBiomeType.Tundra => new[] { EVoxelType.Snow, EVoxelType.Ice, EVoxelType.Stone },
                EBiomeType.Volcano => new[] { EVoxelType.Basalt, EVoxelType.Lava },
                EBiomeType.Swamp => new[] { EVoxelType.Mud, EVoxelType.Water, EVoxelType.Grass },
                _ => new[] { EVoxelType.Dirt, EVoxelType.Stone }
            };
        }

        /// <summary>
        /// Gibt die typischen Erz-VoxelTypes für dieses Biom zurück
        /// </summary>
        public static EVoxelType[] GetOreTypes(this EBiomeType biome)
        {
            return biome switch
            {
                EBiomeType.Mountain => new[] { EVoxelType.IronOre, EVoxelType.Coal, EVoxelType.Stone },
                EBiomeType.IronRichMountain => new[] { EVoxelType.IronOre, EVoxelType.IronOre, EVoxelType.Coal }, // Doppelt für höhere Chance
                EBiomeType.Desert => new[] { EVoxelType.GoldSand, EVoxelType.CopperOre },
                EBiomeType.GoldRichDesert => new[] { EVoxelType.GoldOre, EVoxelType.GoldSand, EVoxelType.SilverOre },
                EBiomeType.Volcano => new[] { EVoxelType.Basalt, EVoxelType.IronOre, EVoxelType.CopperOre },
                _ => new[] { EVoxelType.Stone, EVoxelType.Coal }
            };
        }

        /// <summary>
        /// Gibt die Wahrscheinlichkeit für Gasemissionen in diesem Biom zurück (0.0 - 1.0)
        /// </summary>
        public static float GetGasEmissionProbability(this EBiomeType biome)
        {
            return biome switch
            {
                EBiomeType.Swamp => 0.8f,    // Hohe Faulgase
                EBiomeType.Volcano => 0.9f,   // Vulkanische Gase
                EBiomeType.Cave => 0.3f,      // Eingeschlossene Gase
                _ => 0.1f
            };
        }
    }
}